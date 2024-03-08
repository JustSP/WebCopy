using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace webcopy
{
    public enum ProcessCommand
    {
        Idle = 0,
        Analyzing = 1,
        Analyzed = 2,
        Downloading = 3
    }

    public partial class frmMain : Form
    {
        ProcessCommand _copyState = ProcessCommand.Idle;

        CopyManager copyManager;

        Queue<string> downloadQ = new Queue<string>();

        public ProcessCommand CopyState
        {
            get
            {
                return _copyState;
            }
            set
            {
                _copyState = value;
                OnCopyStateChanged();
            }
        }

        public event EventHandler CopyStateChanged;

        public frmMain()
        {
            InitializeComponent();

            CopyStateChanged += FrmMain_CopyStateChanged;
        }

        #region Private Functions
        private void ToggleProgress()
        {
            pIndicator.Visible = !pIndicator.Visible;
        }

        private void ToggleCopyButton()
        {
            if (copyManager != null)
            {
                chkSingleLevel.Enabled = !chkSingleLevel.Enabled;
                btnCopy.Text = btnCopy.Text.ToLowerInvariant().Contains("copy") ? "Cancel" : "Copy";
                CopyState = ProcessCommand.Analyzing;
            }
        }

        private void Write2Log (string Message)
        {
            txtLog.AppendText(Message + Environment.NewLine);
        }

        private void Add2Pages (string page)
        {
            lstPages.Items.Add(page);
        }

        protected virtual void OnCopyStateChanged()
        {
            CopyStateChanged(this, null);
        }

        private void FrmMain_CopyStateChanged(object sender, EventArgs e)
        {
            this.Text = CopyState.ToString();
        }
        #endregion

        private void btnCopy_Click(object sender, EventArgs e)
        {
            try
            {
                if (CopyState == ProcessCommand.Idle)
                {
                    copyManager = new CopyManager(txtURL.Text.Trim());
                    ToggleCopyButton();
                    ToggleProgress();

                    bgProcess.RunWorkerAsync();
                }
            }
            catch (Exception ex)
            {
                Write2Log(ex.Message);
                copyManager = null;
            }
        }

        private void bgProcess_DoWork(object sender, DoWorkEventArgs e)
        {
            switch (CopyState)
            {
                case ProcessCommand.Idle:
                    break;
                case ProcessCommand.Analyzing:
                    bgProcess.ReportProgress(0, CopyState.ToString());
                    string title = copyManager.GetSiteTitle();
                    e.Result = title;
                    break;
                case ProcessCommand.Analyzed:
                    CopySite(e.Argument as string);
                    break;
                case ProcessCommand.Downloading:
                    break;
            }
        }

        private void bgProcess_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            switch (CopyState)
            {
                case ProcessCommand.Idle:
                    break;
                case ProcessCommand.Analyzing:
                    Write2Log((string)e.UserState);
                    break;
                case ProcessCommand.Analyzed:
                    this.Text = downloadQ.Count.ToString();
                    SiteFile result = e.UserState as SiteFile;
                    Write2Log(string.Format("{0} - {1}", result.Filename, result.Downloaded ? "Downloaded successfully" : "Failed to download"));
                    if (result.Downloaded)
                        Add2Pages(result.Filename);
                    break;
                case ProcessCommand.Downloading:
                    break;
            }
        }

        private void bgProcess_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            switch (CopyState)
            {
                case ProcessCommand.Idle:
                    break;
                case ProcessCommand.Analyzing:
                    Write2Log(string.Format ("Site: {0}", (string)e.Result));
                    CopyState = ProcessCommand.Analyzed;
                    bgProcess.RunWorkerAsync(txtURL.Text);
                    break;
                case ProcessCommand.Analyzed:
                    ToggleCopyButton();
                    ToggleProgress();
                    CopyState = ProcessCommand.Idle;
                    break;
                case ProcessCommand.Downloading:
                    break;
            }
        }

        private void CopySite (string startPage)
        {
            downloadQ.Enqueue(startPage);
            List<string> downloadedPages = new List<string>();

            do
            {
                CopyPageResult result = copyManager.CopyPage(downloadQ.Dequeue());
                bgProcess.ReportProgress(0, result.Page);

                if (result.Page.Downloaded)
                    downloadedPages.Add(result.Page.Filename);

                foreach (string page in result.Links.Distinct ().ToArray())
                {
                    if ((!downloadedPages.Contains(page)) & (!downloadQ.Contains(page)))
                        downloadQ.Enqueue(page);
                }

                //downloadQ.Clear ();
                //downloadQ = new Queue<string>(downloadQ.Distinct());

            } while (downloadQ.Count > 0);
            
        }
    }
}
