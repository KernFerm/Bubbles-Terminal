using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace BubblesCmd.Core.Services;

public sealed class ConPtySession : IDisposable
{
    private const int ProcThreadAttributePseudoConsole = 0x00020016;
    private const uint ExtendedStartupInfoPresent = 0x00080000;
    private const uint CreateUnicodeEnvironment = 0x00000400;
    private readonly UTF8Encoding _utf8NoBom = new(false, false);
    private readonly SafeFileHandle _inputReadHandle;
    private readonly SafeFileHandle _inputWriteHandle;
    private readonly SafeFileHandle _outputReadHandle;
    private readonly SafeFileHandle _outputWriteHandle;
    private readonly FileStream _inputStream;
    private readonly FileStream _outputStream;
    private readonly IntPtr _pseudoConsole;
    private readonly PROCESS_INFORMATION _processInformation;
    private readonly CancellationTokenSource _lifetimeCts = new();
    private readonly Task _outputTask;
    private bool _disposed;

    private ConPtySession(
        SafeFileHandle inputReadHandle,
        SafeFileHandle inputWriteHandle,
        SafeFileHandle outputReadHandle,
        SafeFileHandle outputWriteHandle,
        IntPtr pseudoConsole,
        PROCESS_INFORMATION processInformation)
    {
        _inputReadHandle = inputReadHandle;
        _inputWriteHandle = inputWriteHandle;
        _outputReadHandle = outputReadHandle;
        _outputWriteHandle = outputWriteHandle;
        _pseudoConsole = pseudoConsole;
        _processInformation = processInformation;
        _inputStream = new FileStream(_inputWriteHandle, FileAccess.Write, 4096, false);
        _outputStream = new FileStream(_outputReadHandle, FileAccess.Read, 4096, false);
        _outputTask = Task.Run(ReadOutputLoopAsync);
    }

    public event EventHandler<string>? OutputReceived;

    public event EventHandler<int>? Exited;

    public int ProcessId => unchecked((int)_processInformation.dwProcessId);

    public static ConPtySession Start(
        string executablePath,
        string arguments,
        string workingDirectory,
        IDictionary<string, string>? environmentOverrides,
        short columns = 140,
        short rows = 40)
    {
        var securityAttributes = new SECURITY_ATTRIBUTES
        {
            nLength = Marshal.SizeOf<SECURITY_ATTRIBUTES>(),
            bInheritHandle = false
        };

        if (!CreatePipe(out var inputReadSide, out var inputWriteSide, ref securityAttributes, 0))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to create ConPTY input pipe.");
        }

        if (!CreatePipe(out var outputReadSide, out var outputWriteSide, ref securityAttributes, 0))
        {
            inputReadSide.Dispose();
            inputWriteSide.Dispose();
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to create ConPTY output pipe.");
        }

        IntPtr pseudoConsole = IntPtr.Zero;
        IntPtr attributeList = IntPtr.Zero;
        IntPtr startupInfoPtr = IntPtr.Zero;
        IntPtr environmentBlock = IntPtr.Zero;
        PROCESS_INFORMATION processInformation = default;

        try
        {
            ThrowOnFailure(CreatePseudoConsole(
                new COORD(columns, rows),
                inputReadSide.DangerousGetHandle(),
                outputWriteSide.DangerousGetHandle(),
                0,
                out pseudoConsole));

            var attributeListSize = IntPtr.Zero;
            InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref attributeListSize);
            attributeList = Marshal.AllocHGlobal(attributeListSize);

            if (!InitializeProcThreadAttributeList(attributeList, 1, 0, ref attributeListSize))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to initialize ConPTY attribute list.");
            }

            if (!UpdateProcThreadAttribute(
                    attributeList,
                    0,
                    (IntPtr)ProcThreadAttributePseudoConsole,
                    pseudoConsole,
                    (IntPtr)IntPtr.Size,
                    IntPtr.Zero,
                    IntPtr.Zero))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to attach ConPTY startup attribute.");
            }

            var startupInfoEx = new STARTUPINFOEX
            {
                StartupInfo = new STARTUPINFO
                {
                    cb = Marshal.SizeOf<STARTUPINFOEX>()
                },
                lpAttributeList = attributeList
            };

            startupInfoPtr = Marshal.AllocHGlobal(Marshal.SizeOf<STARTUPINFOEX>());
            Marshal.StructureToPtr(startupInfoEx, startupInfoPtr, false);

            environmentBlock = BuildEnvironmentBlock(environmentOverrides);
            var commandLine = new StringBuilder(CommandLineBuilder.Build(executablePath, arguments));

            if (!CreateProcessW(
                    null,
                    commandLine,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    true,
                    ExtendedStartupInfoPresent | CreateUnicodeEnvironment,
                    environmentBlock,
                    string.IsNullOrWhiteSpace(workingDirectory) ? null : workingDirectory,
                    startupInfoPtr,
                    out processInformation))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"Unable to launch '{executablePath}'.");
            }

            var session = new ConPtySession(
                inputReadSide,
                inputWriteSide,
                outputReadSide,
                outputWriteSide,
                pseudoConsole,
                processInformation);
            _ = Task.Run(() => session.WaitForExitAsync(session._lifetimeCts.Token));
            return session;
        }
        catch
        {
            inputReadSide.Dispose();
            inputWriteSide.Dispose();
            outputReadSide.Dispose();
            outputWriteSide.Dispose();

            if (processInformation.hThread != IntPtr.Zero)
            {
                CloseHandle(processInformation.hThread);
            }

            if (processInformation.hProcess != IntPtr.Zero)
            {
                CloseHandle(processInformation.hProcess);
            }

            if (pseudoConsole != IntPtr.Zero)
            {
                ClosePseudoConsole(pseudoConsole);
            }

            throw;
        }
        finally
        {
            if (attributeList != IntPtr.Zero)
            {
                DeleteProcThreadAttributeList(attributeList);
                Marshal.FreeHGlobal(attributeList);
            }

            if (startupInfoPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(startupInfoPtr);
            }

            if (environmentBlock != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(environmentBlock);
            }
        }
    }

    public async Task SendInputAsync(string text, CancellationToken cancellationToken = default)
    {
        var bytes = _utf8NoBom.GetBytes(text);
        await _inputStream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
        await _inputStream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public void Resize(short columns, short rows)
    {
        ThrowOnFailure(ResizePseudoConsole(_pseudoConsole, new COORD(columns, rows)));
    }

    public void Terminate()
    {
        if (_processInformation.hProcess != IntPtr.Zero)
        {
            TerminateProcess(_processInformation.hProcess, 1);
        }
    }

    private async Task ReadOutputLoopAsync()
    {
        var buffer = new byte[4096];

        try
        {
            while (!_lifetimeCts.IsCancellationRequested)
            {
                var bytesRead = await _outputStream.ReadAsync(buffer, _lifetimeCts.Token).ConfigureAwait(false);
                if (bytesRead <= 0)
                {
                    break;
                }

                var text = _utf8NoBom.GetString(buffer, 0, bytesRead);
                OutputReceived?.Invoke(this, text);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private async Task WaitForExitAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var waitResult = WaitForSingleObject(_processInformation.hProcess, 200);
                if (waitResult == 0)
                {
                    break;
                }

                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }

            if (GetExitCodeProcess(_processInformation.hProcess, out var exitCode))
            {
                Exited?.Invoke(this, unchecked((int)exitCode));
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _lifetimeCts.Cancel();

        try
        {
            _inputStream.Dispose();
            _outputStream.Dispose();
            _outputTask.Wait(250);
        }
        catch
        {
        }

        _inputWriteHandle.Dispose();
        _inputReadHandle.Dispose();
        _outputReadHandle.Dispose();
        _outputWriteHandle.Dispose();

        if (_processInformation.hThread != IntPtr.Zero)
        {
            CloseHandle(_processInformation.hThread);
        }

        if (_processInformation.hProcess != IntPtr.Zero)
        {
            CloseHandle(_processInformation.hProcess);
        }

        if (_pseudoConsole != IntPtr.Zero)
        {
            ClosePseudoConsole(_pseudoConsole);
        }

        _lifetimeCts.Dispose();
    }

    private static IntPtr BuildEnvironmentBlock(IDictionary<string, string>? environmentOverrides)
    {
        var environment = Environment.GetEnvironmentVariables()
            .Cast<System.Collections.DictionaryEntry>()
            .ToDictionary(
                entry => (string)entry.Key,
                entry => (string?)entry.Value ?? string.Empty,
                StringComparer.OrdinalIgnoreCase);

        if (environmentOverrides is not null)
        {
            foreach (var pair in environmentOverrides)
            {
                environment[pair.Key] = pair.Value;
            }
        }

        var environmentBlock = string.Join(
            '\0',
            environment.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .Select(pair => $"{pair.Key}={pair.Value}")) + "\0\0";

        return Marshal.StringToHGlobalUni(environmentBlock);
    }

    private static void ThrowOnFailure(int hResult)
    {
        if (hResult < 0)
        {
            Marshal.ThrowExceptionForHR(hResult);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct COORD(short x, short y)
    {
        public short X = x;
        public short Y = y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SECURITY_ATTRIBUTES
    {
        public int nLength;
        public IntPtr lpSecurityDescriptor;
        [MarshalAs(UnmanagedType.Bool)]
        public bool bInheritHandle;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public uint dwProcessId;
        public uint dwThreadId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct STARTUPINFO
    {
        public int cb;
        public IntPtr lpReserved;
        public IntPtr lpDesktop;
        public IntPtr lpTitle;
        public int dwX;
        public int dwY;
        public int dwXSize;
        public int dwYSize;
        public int dwXCountChars;
        public int dwYCountChars;
        public int dwFillAttribute;
        public int dwFlags;
        public short wShowWindow;
        public short cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct STARTUPINFOEX
    {
        public STARTUPINFO StartupInfo;
        public IntPtr lpAttributeList;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CreatePipe(
        out SafeFileHandle hReadPipe,
        out SafeFileHandle hWritePipe,
        ref SECURITY_ATTRIBUTES lpPipeAttributes,
        int nSize);

    [DllImport("kernel32.dll", EntryPoint = "CreatePseudoConsole")]
    private static extern int CreatePseudoConsole(
        COORD size,
        IntPtr hInput,
        IntPtr hOutput,
        uint dwFlags,
        out IntPtr phPC);

    [DllImport("kernel32.dll", EntryPoint = "ResizePseudoConsole")]
    private static extern int ResizePseudoConsole(IntPtr hPC, COORD size);

    [DllImport("kernel32.dll", EntryPoint = "ClosePseudoConsole")]
    private static extern void ClosePseudoConsole(IntPtr hPC);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool InitializeProcThreadAttributeList(
        IntPtr lpAttributeList,
        int dwAttributeCount,
        int dwFlags,
        ref IntPtr lpSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UpdateProcThreadAttribute(
        IntPtr lpAttributeList,
        uint dwFlags,
        IntPtr attribute,
        IntPtr lpValue,
        IntPtr cbSize,
        IntPtr lpPreviousValue,
        IntPtr lpReturnSize);

    [DllImport("kernel32.dll")]
    private static extern void DeleteProcThreadAttributeList(IntPtr lpAttributeList);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CreateProcessW(
        string? lpApplicationName,
        StringBuilder lpCommandLine,
        IntPtr lpProcessAttributes,
        IntPtr lpThreadAttributes,
        [MarshalAs(UnmanagedType.Bool)] bool bInheritHandles,
        uint dwCreationFlags,
        IntPtr lpEnvironment,
        string? lpCurrentDirectory,
        IntPtr lpStartupInfo,
        out PROCESS_INFORMATION lpProcessInformation);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetExitCodeProcess(IntPtr hProcess, out uint lpExitCode);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr handle);
}
