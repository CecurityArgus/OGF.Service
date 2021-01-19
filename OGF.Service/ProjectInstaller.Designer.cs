namespace OGF.Service
{
    partial class ProjectInstaller
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.serviceProcess = new System.ServiceProcess.ServiceProcessInstaller();
            this.serviceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // serviceProcess
            // 
            this.serviceProcess.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.serviceProcess.Password = null;
            this.serviceProcess.Username = null;
            this.serviceProcess.AfterInstall += new System.Configuration.Install.InstallEventHandler(this.serviceProcess_AfterInstall);
            // 
            // serviceInstaller
            // 
            this.serviceInstaller.Description = "OGF Service";
            this.serviceInstaller.DisplayName = "OGF Service";
            this.serviceInstaller.ServiceName = "OGF Service";
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.serviceProcess,
            this.serviceInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller serviceProcess;
        internal System.ServiceProcess.ServiceInstaller serviceInstaller;
    }
}