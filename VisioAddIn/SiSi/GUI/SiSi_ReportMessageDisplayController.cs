using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace VisioAddIn.SiSi.GUI
{
    public class SiSi_ReportMessageDisplayController
    {

        private SiSi_ReportMessageDisplayWindow window;
        private List<SiSi_ReportMessage> reportMessageList;
        private ListView listview;

        public SiSi_ReportMessageDisplayController()
        {
            window = new SiSi_ReportMessageDisplayWindow(this);
            listview = window.getListView();
            initializeListView();
            refresh();
            window.Show();
        }

        private void initializeListView()
        {
            listview.View = View.Details;
            listview.Columns.Add("ReportType", 150);
            listview.Columns.Add("ShapeType", 150);
            listview.Columns.Add("Shapename", 200);
            listview.Columns.Add("Message", 500);
            listview.Columns.Add("Time since Start", 100);
            listview.Columns.Add("Time", 100);
            listview.Columns.Add("devCode", 5);
        }

        private ListViewItem createListViewItem(SiSi_ReportMessage msg)
        {
            string reporttype, shapetype, shapename, message, devcode;
            DateTime time;
            msg.giveAllInfos(out reporttype, out shapetype, out shapename, out message, out devcode, out time);
            string[] info = new string[] { reporttype, shapetype, shapename, message, (time - SiSi_SimpleSim.start).ToString(@"d\.hh\:mm\:ss\.fffff"), time.ToString("hh:mm:ss.fffff"), devcode };
            return new ListViewItem(info);
        }
        internal void refresh()
        {
            reportMessageList = SiSi_SimpleSim.getReportMessageList();
            listview.Items.Clear();
            parseListIntoListView();
        }

        private void parseListIntoListView()
        {
            if (reportMessageList != null)
            {
                foreach (SiSi_ReportMessage msg in reportMessageList)
                {
                    listview.Items.Add(createListViewItem(msg));
                }
            }
        }
    }
}
