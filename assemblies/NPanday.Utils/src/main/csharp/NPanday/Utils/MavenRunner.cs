using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using EnvDTE;
using EnvDTE80;
using System.Runtime.CompilerServices;

using System.Threading;
using System.Diagnostics;



namespace NPanday.Utils
{
    public class MavenRunner
    {
        OutputWindowPane output;
        System.Diagnostics.Process currentProcess;
        DTE2 dte2;
        bool stopCalled;
        System.Threading.Thread outputThread;
        bool running;
        ManualResetEvent outputThreadEvent;
        public event EventHandler RunnerStopped;


        protected void onRunnerStopped()
        {
            if (RunnerStopped != null)
                RunnerStopped(this, new EventArgs());
        }

        public MavenRunner(DTE2 dte2)
        {
            this.dte2 = dte2;
            output = MakeOutputWindow();
            outputThreadEvent = null;

            // create a worker thread for outputing the process console out puts
            running = true;
            System.Threading.ThreadStart outputThreadStart = new System.Threading.ThreadStart(OutputThreadDelegate);
            outputThread = new System.Threading.Thread(outputThreadStart);
            outputThread.Start();
        }

		public void ClearOutputWindow()
		{
			output.Clear();
		}
		
        public void Quit()
        {
            running = false;
            if (IsRunning)
            {
                stop();
            }
        }

		private void DeleteBinDir()
        {
            Solution2 solution = (Solution2)dte2.Solution;

            Projects projs = solution.Projects;

            bool isFlatProject = true;
            
                string[] directoryPartial = solution.FullName.Split("\\".ToCharArray());
                string pathPartial = directoryPartial[directoryPartial.Length - 1];
                string path = solution.FullName.Substring(0, solution.FullName.Length - pathPartial.Length);
                
                path = path.Replace("\\", "//");
                string baseDirectory = path;
                path = path + "/bin";


                string[] directories = Directory.GetDirectories(baseDirectory);
                foreach (string dir in directories)
                {
                    string[] dirFiles = Directory.GetFiles(dir);
                    foreach (string f in dirFiles)
                    {
                        if (f.Contains("pom.xml"))
                        {
                            isFlatProject = false;
                            break;
                        }
                    }
                    if (!isFlatProject)
                    {
                        break;
                    }
                }

                if (Directory.Exists(path) && !isFlatProject)
                {
                    Directory.Delete(path, true);
                }
          
		        try
                {
                    string tempDir20 = "C:\\WINDOWS\\Microsoft.NET\\Framework\\v2.0.50727\\Temporary ASP.NET Files\\NPanday_Temp";


                    if (Directory.Exists(tempDir20))
                    {
                        Directory.Delete(tempDir20, true);
                    }
                }
                catch (Exception e)
                { 
                    //deletes temp directories for compilation
                }

                try
                {
                    string tempDir30 = "C:\\WINDOWS\\Microsoft.NET\\Framework\\v3.0\\Temporary ASP.NET Files\\NPanday_Temp";


                    if (Directory.Exists(tempDir30))
                    {
                        Directory.Delete(tempDir30, true);
                    }
                }
                catch (Exception e)
                {
                    //deletes temp directories for compilation
                }

                try
                {
                    string tempDir35 = "C:\\WINDOWS\\Microsoft.NET\\Framework\\v3.5\\Temporary ASP.NET Files\\NPanday_Temp";


                    if (Directory.Exists(tempDir35))
                    {
                        Directory.Delete(tempDir35, true);
                    }
                }
                catch (Exception e)
                {
                    //deletes temp directories for compilation
                }
          

            
        }

		
        private void OutputThreadDelegate()
        {

            while (running)
            {
                // assign to a local variable to avoid raise exception
                System.Diagnostics.Process proc = currentProcess;
                
                
                
                if (!IsRunning)
                {
                    // no process, make the thread pasivate to save cpu usage;
                    outputThreadEvent = new ManualResetEvent(false);
                    outputThreadEvent.WaitOne();
                    continue;
                }

                StreamReader mvnOutput = proc.StandardOutput;
                if (mvnOutput.Peek() != 0)
                {
                    string value = mvnOutput.ReadLine();
                    if (!string.IsNullOrEmpty(value) && !"".Equals(value.Trim()))
                    {
                        if(!stopCalled)
                            output.OutputString("\n" + value);
                    }
                }
            }

        }

        private OutputWindowPane MakeOutputWindow()
        {
           // _applicationObject is from the main class
            try
            {
                Window win = dte2.Windows.Item(EnvDTE.Constants.vsWindowKindOutput);
                OutputWindow outputWindow = (OutputWindow)win.Object;
                OutputWindowPane outputPane = null;

                OutputWindowPanes panes = outputWindow.OutputWindowPanes;

                // Reuse the existing pane (if it exists)

                for (int i = 1; i <= panes.Count; i++)
                {
                    outputPane = panes.Item(i);
                    if (outputPane.Name == "NPanday Execution Output:")
                        return outputPane;
                }

                OutputWindowPane output = outputWindow.OutputWindowPanes.Add("NPanday Execution Output:");
                return output;
            }
            catch (Exception e)
            {
                throw new Exception("Error In Generation Output Window: " + e.Message);
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void InitializeMavenRunner()
        {
            output.Clear();
            output.Activate();
            stopCalled = false;
        }

        public void execute(string pomFile, string goal)
        {
            execute(pomFile, goal, null);
        }

        public void execute(string pomFile, string goal, string[] parameters)
        {
            InitializeMavenRunner();

            if (!(new FileInfo(pomFile)).Exists)
            {
                string errStr = string.Format("Error: Pom File {0} not found!", pomFile);
                output.OutputString(errStr);
                throw new Exception(errStr);
            }

            List<string> paramList = new List<string>();
            paramList.Add(pomFile);
            paramList.Add(goal);

            if (!"pom.xml".Equals(Path.GetFileName(pomFile), StringComparison.OrdinalIgnoreCase))
            {
                paramList.Add(string.Format("-f\"{0}\"", Path.GetFileName(pomFile)));
            }

            if (parameters != null)
            {
                paramList.AddRange(parameters);
            }


            ExecuteMaven(paramList.ToArray());
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void ExecuteMaven(string[] param)
        {
            // use local variable to avoid raise exception
            System.Diagnostics.Process process = null;

            try
            {
                process = StartNewMavenProcess((string[])param);
                currentProcess = process;
                if (outputThreadEvent != null)
                {
                    outputThreadEvent.Set();
                }
                
            }
            catch (Exception e)
            {
                output.OutputString("Error in Starting Maven Process: " + e.Message);
                return;
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private System.Diagnostics.Process StartNewMavenProcess(string[] args)
        {

            if (this.IsRunning)
            {
                throw new Exception("A Maven: Process Is still Running!");
            }

            string pomFile = args[0];
            string goal = args[1];
            string arguments = null;

            if (args.Length > 2)
            {
                arguments = string.Join(" ", args, 2, args.Length - 2);
            }


            

            if (!string.IsNullOrEmpty(arguments))
            {
                arguments = string.Format("{0} {1}", goal, arguments);
            }
            else
            {
                arguments = goal;
            }


            System.Diagnostics.Process process = new System.Diagnostics.Process();


            string mvn_file = string.Format("{0}\\bin\\mvn.bat", System.Environment.GetEnvironmentVariable("M2_HOME"));
           
            output.OutputString("\n------------------------------------------------------------------");
            output.OutputString("\nExecuting Maven");
            output.OutputString("\nPom File: " + pomFile);
            output.OutputString("\nGoal: " + goal);
            output.OutputString("\nArguments: " + arguments);
            output.OutputString(string.Format("\nNPanday Command: {0} {1}\n\n", mvn_file, arguments));
            output.OutputString("\n------------------------------------------------------------------\n\n");


            System.Diagnostics.ProcessStartInfo procInfo = new System.Diagnostics.ProcessStartInfo(mvn_file);
            procInfo.Arguments = arguments;
            procInfo.WorkingDirectory = Path.GetDirectoryName(pomFile);

            procInfo.RedirectStandardOutput = true;
            procInfo.RedirectStandardError = true;
            procInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            procInfo.CreateNoWindow = true;
            procInfo.UseShellExecute = false;


            process.StartInfo = procInfo;

            process.EnableRaisingEvents = true;
            process.Exited += new EventHandler(mvn_process_exited);

            process.Start();
            return process;
        }


        public bool IsRunning
        {
            get
            {
                if (currentProcess == null)
                    return false;

                return !currentProcess.HasExited;
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void stop()
        {
            try
            {
                stopCalled = true;
                output.OutputString("\nStopping current NPanday process.");
                if (outputThreadEvent != null)
                {
                    outputThreadEvent.WaitOne();
                }
                currentProcess.Kill();
                currentProcess.WaitForExit();                
            }
            catch (Exception e)
            {

                output.OutputString("Error in Stopping Maven Process: " + e.Message);
            }


        }

        private void mvn_process_exited(object sender, System.EventArgs e)
        {
            System.Diagnostics.Process process = (System.Diagnostics.Process)sender;

            // flush the remaining output
            StreamReader mvnOutput = process.StandardOutput;

            if (stopCalled)
            {
                output.OutputString("\nNPanday execution stopped successfully.");
                onRunnerStopped();
                return;
            }

            if (mvnOutput.Peek() != 0)
            {
                string value = mvnOutput.ReadToEnd();
                if (!string.IsNullOrEmpty(value) && !"".Equals(value.Trim()))
                {
                    output.OutputString("\n" + value);
                }
            }


            
            int exitCode = process.ExitCode;
            if (exitCode == 0)
            {
                output.OutputString("\nNPanday Execution is Successful!");
				DeleteBinDir();
            }
            else
            {
                output.OutputString("\nNPanday Execution Failed!, with exit code: " + exitCode);
                DeleteBinDir();
            }
            onRunnerStopped();
            // dont display any failed execution if stop
            //else if (exitCode == -1)
            //{
            //    output.OutputString("\nNPanday Execution Failed!");
            //}
        }
    }
}
