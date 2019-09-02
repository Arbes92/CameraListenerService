namespace CameraListenerService
{
    partial class CameraListenerService
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
            this.components = new System.ComponentModel.Container();
            this.Timer_Cleanup = new System.Windows.Forms.Timer(this.components);
            this.Timer_Parse = new System.Windows.Forms.Timer(this.components);
            // 
            // Timer_Cleanup
            // 
            this.Timer_Cleanup.Interval = 60000;
            this.Timer_Cleanup.Tick += new System.EventHandler(this.Timer_Cleanup_Tick);
            // 
            // Timer_Parse
            // 
            this.Timer_Parse.Interval = 60000;
            this.Timer_Parse.Tick += new System.EventHandler(this.Timer_Parse_Tick);
            // 
            // CameraListenerService
            // 
            this.ServiceName = "Service1";

        }

        #endregion

        private System.Windows.Forms.Timer Timer_Cleanup;
        private System.Windows.Forms.Timer Timer_Parse;
    }
}
