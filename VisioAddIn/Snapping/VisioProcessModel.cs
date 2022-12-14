using System.Collections.Generic;
using System.Linq;

namespace VisioAddIn.Snapping
{
    /// <summary>
    /// represent a process model
    /// </summary>
    public class VisioProcessModel : IVisioProcessModel
    {
        private readonly string modelUri;
        private IList<SIDPage> sidPages;
        //TODO maybe updateWholeController after drag&drop etc?!
        private int currentPriority;

        public VisioProcessModel(string modelUri)
        {
            this.modelUri = modelUri;
            sidPages = new List<SIDPage>();
            currentPriority = 10;
        }

        public IDictionary<SIDPage, IList<SBDPage>> getTreeView()
        {
            IDictionary<SIDPage, IList<SBDPage>> treeView = new Dictionary<SIDPage, IList<SBDPage>>();
            foreach (SIDPage page in sidPages)
            {
                treeView.Add(page, page.getTreeView());
            }
            return treeView;
        }

        public int getCurrentPriority()
        {
            currentPriority += 10;
            return currentPriority;
        }

        public IList<SIDPage> getSidPages()
        {
            return this.sidPages;
        }

        public bool containsSidPage(string pageNameU)
        {
            return getSidPages().Any(sidPage => sidPage.getNameU().Equals(pageNameU));
        }

        public string getModelUri()
        {
            return this.modelUri;
        }

        public void removePage(SIDPage page)
        {
            sidPages.Remove(page);
        }

        /// <summary>
        /// adds an existing sid page to this model
        /// </summary>
        /// <param name="page">page to be added</param>
        /// TODO maybe check if name is unique
        public void addSidPage(SIDPage page)
        {
            sidPages.Add(page);

            // Why is no sort on IList available?!
            List<SIDPage> tmp = new List<SIDPage>(sidPages);
            tmp.Sort();
            sidPages = tmp;
        }
    }

    public interface IVisioProcessModel
    {
        int getCurrentPriority();

        bool containsSidPage(string pageNameU);

        string getModelUri();

        void removePage(SIDPage page);

        /// <summary>
        /// adds an existing sid page to this model
        /// </summary>
        /// <param name="page">page to be added</param>
        /// TODO maybe check if name is unique
        void addSidPage(SIDPage page);

        IList<SIDPage> getSidPages();

        IDictionary<SIDPage, IList<SBDPage>> getTreeView();
    }
}

