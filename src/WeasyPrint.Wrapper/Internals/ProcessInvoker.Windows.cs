﻿namespace WeasyPrint.Wrapper.Internals
{
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Runtime.InteropServices;

    internal partial class ProcessInvoker
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GenerateConsoleCtrlEvent(ConsoleCtrlEvent sigevent, int dwProcessGroupId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate HandlerRoutine, bool Add);

        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern int NtQueryInformationProcess(IntPtr processHandle, PROCESSINFOCLASS processInformationClass, ref PROCESS_BASIC_INFORMATION processInformation, int processInformationLength, ref int returnLength);

        private void WindowsKillProcessTree()
        {
            var pid = _proc?.Id;
            if (pid == null)
            {
                // process already exit, stop here.
                return;
            }

            Dictionary<int, int> processRelationship = new Dictionary<int, int>();

            //Trace.Info($"Scan all processes to find relationship between all processes.");

            foreach (Process proc in Process.GetProcesses())
            {
                try
                {
                    if (!proc.SafeHandle.IsInvalid)
                    {
                        PROCESS_BASIC_INFORMATION pbi = new PROCESS_BASIC_INFORMATION();
                        int returnLength = 0;
                        int queryResult = NtQueryInformationProcess(proc.SafeHandle.DangerousGetHandle(), PROCESSINFOCLASS.ProcessBasicInformation, ref pbi, Marshal.SizeOf(pbi), ref returnLength);
                        if (queryResult == 0) // == 0 is OK
                        {
                            //Trace.Verbose($"Process: {proc.Id} is child process of {pbi.InheritedFromUniqueProcessId}.");

                            processRelationship[proc.Id] = (int)pbi.InheritedFromUniqueProcessId;
                        }
                        else
                        {
                            throw new Win32Exception(Marshal.GetLastWin32Error());
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Ignore all exceptions, since KillProcessTree is best effort.
                    //Trace.Verbose("Ignore any catched exception during detecting process relationship.");
                    //Trace.Verbose(ex.ToString());
                    _trace?.Info(ex.Message);
                }
            }

            //Trace.Verbose($"Start killing process tree of process '{pid.Value}'.");

            Stack<ProcessTerminationInfo> processesNeedtoKill = new Stack<ProcessTerminationInfo>();

            processesNeedtoKill.Push(new ProcessTerminationInfo(pid.Value, false));
            while (processesNeedtoKill.Count() > 0)
            {
                ProcessTerminationInfo procInfo = processesNeedtoKill.Pop();
                List<int> childProcessesIds = new List<int>();
                if (!procInfo.ChildPidExpanded)
                {
                    //Trace.Info($"Find all child processes of process '{procInfo.Pid}'.");
                    childProcessesIds = processRelationship.Where(p => p.Value == procInfo.Pid).Select(k => k.Key).ToList();
                }

                if (childProcessesIds.Count > 0)
                {
                    //Trace.Info($"Need kill all child processes trees before kill process '{procInfo.Pid}'.");
                    processesNeedtoKill.Push(new ProcessTerminationInfo(procInfo.Pid, true));
                    foreach (var childPid in childProcessesIds)
                    {
                        //Trace.Info($"Child process '{childPid}' needs be killed first.");
                        processesNeedtoKill.Push(new ProcessTerminationInfo(childPid, false));
                    }
                }
                else
                {
                    //Trace.Info($"Kill process '{procInfo.Pid}'.");

                    try
                    {
                        Process leafProcess = Process.GetProcessById(procInfo.Pid);
                        try
                        {
                            leafProcess.Kill();
                        }
                        catch (InvalidOperationException ex)
                        {
                            // The process has already exited
                            //Trace.Verbose("Ignore InvalidOperationException during Process.Kill().");
                            //Trace.Verbose(ex.ToString());
                            _trace?.Info(ex.Message);
                        }
                        catch (Win32Exception ex) when (ex.NativeErrorCode == 5)
                        {
                            // The associated process could not be terminated
                            // The process is terminating
                            // NativeErrorCode 5 means Access Denied
                            //Trace.Verbose("Ignore Win32Exception with NativeErrorCode 5 during Process.Kill().");
                            //Trace.Verbose(ex.ToString());
                            _trace?.Info(ex.Message);
                        }
                        catch (Exception ex)
                        {
                            // Ignore any additional exception
                            //Trace.Verbose("Ignore additional exceptions during Process.Kill().");
                            //Trace.Verbose(ex.ToString());
                            _trace?.Info(ex.Message);
                        }
                    }
                    catch (ArgumentException ex)
                    {
                        // process already gone, nothing needs killed.
                        //Trace.Verbose("Ignore ArgumentException during Process.GetProcessById().");
                        //Trace.Verbose(ex.ToString());
                        _trace?.Info(ex.Message);
                    }
                    catch (Exception ex)
                    {
                        // Ignore any additional exception
                        //Trace.Verbose("Ignore additional exceptions during Process.GetProcessById().");
                        //Trace.Verbose(ex.ToString());
                        _trace?.Info(ex.Message);
                    }
                }
            }
        }

        private async Task<bool> SendCtrlSignal(ConsoleCtrlEvent signal, TimeSpan timeout)
        {
            //Trace.Info($"Sending {signal} to process {_proc.Id}.");
            ConsoleCtrlDelegate ctrlEventHandler = new ConsoleCtrlDelegate(ConsoleCtrlHandler);
            try
            {
                if (!FreeConsole())
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                if (!AttachConsole(_proc!.Id))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                if (!SetConsoleCtrlHandler(ctrlEventHandler, true))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                if (!GenerateConsoleCtrlEvent(signal, 0))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                //Trace.Info($"Successfully send {signal} to process {_proc.Id}.");
                //Trace.Info($"Waiting for process exit or {timeout.TotalSeconds} seconds after {signal} signal fired.");
                var completedTask = await Task.WhenAny(Task.Delay(timeout), _processExitedCompletionSource.Task);
                if (completedTask == _processExitedCompletionSource.Task)
                {
                    //Trace.Info("Process exit successfully.");
                    return true;
                }
                else
                {
                    //Trace.Info($"Process did not honor {signal} signal within {timeout.TotalSeconds} seconds.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                //Trace.Info($"{signal} signal doesn't fire successfully.");
                //Trace.Verbose($"Catch exception during send {signal} event to process {_proc.Id}");
                //Trace.Verbose(ex.ToString());
                _trace?.Info(ex.Message);
                return false;
            }
            finally
            {
                FreeConsole();
                SetConsoleCtrlHandler(ctrlEventHandler, false);
            }
        }

        private bool ConsoleCtrlHandler(ConsoleCtrlEvent ctrlType)
        {
            switch (ctrlType)
            {
                case ConsoleCtrlEvent.CTRL_C:
                    _trace?.Info($"Ignore Ctrl+C to current process.");
                    return true;
                case ConsoleCtrlEvent.CTRL_BREAK:

                    _trace?.Info($"Ignore Ctrl+Break to current process.");
                    return true;
            }

            return false;
        }
    }
}
