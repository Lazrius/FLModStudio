﻿using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using FreelancerModStudio.Data;
using FreelancerModStudio.SystemPresenter;
using HelixEngine;
using WeifenLuo.WinFormsUI.Docking;

namespace FreelancerModStudio
{
    public partial class frmSystemEditor : DockContent, IContentForm
    {
        Presenter systemPresenter;
        Thread universeLoadingThread;

        public delegate void SelectionChangedType(int id);
        public SelectionChangedType SelectionChanged;

        void OnSelectionChanged(int id)
        {
            if (SelectionChanged != null)
                SelectionChanged(id);
        }

        public Presenter.FileOpenType FileOpen;

        void OnFileOpen(string file)
        {
            if (FileOpen != null)
                FileOpen(file);
        }

        public frmSystemEditor()
        {
            InitializeComponent();
            InitializeView();
        }

        void InitializeView()
        {
#if DEBUG
            Stopwatch st = new Stopwatch();
            st.Start();
#endif
            //create viewport using the Helix Engine
            HelixViewport3D viewport = new HelixViewport3D
                {
                    Background = Brushes.Black,
                    Foreground = Brushes.White,
                    FontSize = 16,
                    ClipToBounds = false,
                    ShowViewCube = true
                };
#if DEBUG
            st.Stop();
            Debug.WriteLine("init HelixView: " + st.ElapsedMilliseconds + "ms");
            st.Start();
#endif
            ElementHost host = new ElementHost
                {
                    Child = viewport,
                    Dock = DockStyle.Fill
                };
            Controls.Add(host);
#if DEBUG
            st.Stop();
            Debug.WriteLine("init host: " + st.ElapsedMilliseconds + "ms");
#endif
            systemPresenter = new Presenter(viewport);
            systemPresenter.SelectionChanged += systemPresenter_SelectionChanged;
            systemPresenter.FileOpen += systemPresenter_FileOpen;
        }

        void systemPresenter_SelectionChanged(ContentBase content)
        {
            OnSelectionChanged(content.ID);
        }

        void systemPresenter_FileOpen(string file)
        {
            OnFileOpen(file);
        }

        public void ShowData(TableData data, string file, ArchetypeManager archetype)
        {
            Helper.Thread.Abort(ref universeLoadingThread, false);

            Clear();
            systemPresenter.Add(data.Blocks);

            if (archetype != null)
            {
                DisplayUniverse(file, data.Blocks, archetype);
            }
            else
            {
                systemPresenter.IsUniverse = false;
            }
        }

        void DisplayUniverse(string file, List<TableBlock> blocks, ArchetypeManager archetype)
        {
            systemPresenter.IsUniverse = true;
            if (File.Exists(file))
            {
                string path = Path.GetDirectoryName(file);

                ThreadStart threadStart = delegate { systemPresenter.DisplayUniverse(path, Helper.Template.Data.SystemFile, blocks, archetype); };

                Helper.Thread.Start(ref universeLoadingThread, threadStart, ThreadPriority.Normal, true);
            }
        }

        public new void Dispose()
        {
            base.Dispose();

            if (systemPresenter != null)
            {
                Clear(true);
            }
        }

        void Clear(bool clearLight)
        {
            Helper.Thread.Abort(ref universeLoadingThread, true);

            systemPresenter.ClearDisplay(clearLight);
        }

        public void Clear()
        {
            Clear(false);
        }

        ContentBase GetContent(int id)
        {
            for (int i = systemPresenter.GetContentStartId(); i < systemPresenter.Viewport.Children.Count; i++)
            {
                ContentBase content = (ContentBase)systemPresenter.Viewport.Children[i];
                if (content.ID == id)
                {
                    return content;
                }
            }
            return null;
        }

        public void Select(TableBlock block)
        {
            // always set title from table editor selection change
            systemPresenter.Viewport.Title = block.Name;

            // return if object is already selected
            if (systemPresenter.SelectedContent != null && systemPresenter.SelectedContent.ID == block.UniqueID)
            {
                return;
            }

            ContentBase content = GetContent(block.UniqueID);
            if (content != null)
            {
                systemPresenter.SelectedContent = content;
            }
            else
            {
                Deselect();
            }
        }

        public void Deselect()
        {
            systemPresenter.SelectedContent = null;
        }

        public void SetVisibility(TableBlock block)
        {
            if (block.Visibility)
            {
                systemPresenter.Add(block);
            }
            else
            {
                ContentBase content = GetContent(block.UniqueID);
                if (content != null)
                {
                    systemPresenter.Delete(content);
                }
            }
        }

        public void SetValues(List<TableBlock> blocks)
        {
            List<TableBlock> newBlocks = new List<TableBlock>();

            foreach (TableBlock block in blocks)
            {
                ContentBase content = GetContent(block.UniqueID);
                if (content != null)
                {
                    // visual is visibile as it was found so we need to remove it
                    if (block.Visibility)
                    {
                        systemPresenter.ChangeValues(content, block);
                    }
                    else
                    {
                        systemPresenter.Delete(content);
                    }
                }
                else
                {
                    newBlocks.Add(block);
                }
            }

            if (newBlocks.Count > 0)
            {
                Add(newBlocks);
            }
        }

        public void Add(List<TableBlock> blocks)
        {
            systemPresenter.Add(blocks);
        }

        public void AddModel(Visual3D v)
        {
            systemPresenter.Viewport.Children.Add(v);
        }

        public void Delete(List<TableBlock> blocks)
        {
            foreach (TableBlock block in blocks)
            {
                ContentBase content = GetContent(block.UniqueID);
                if (content != null)
                {
                    systemPresenter.Delete(content);
                }
            }
        }

        public bool UseDocument()
        {
            return true;
        }

        public void FocusSelected()
        {
            if (systemPresenter.SelectedContent != null)
            {
                systemPresenter.LookAt(systemPresenter.SelectedContent);
            }
        }

        #region ContentInterface Members

        public bool CanCopy()
        {
            return false;
        }

        public bool CanCut()
        {
            return false;
        }

        public bool CanPaste()
        {
            return false;
        }

        public bool CanAdd()
        {
            return false;
        }

        public bool CanDelete()
        {
            return false;
        }

        public bool CanSelectAll()
        {
            return false;
        }

        public ToolStripDropDown MultipleAddDropDown()
        {
            return null;
        }

        public void Add(int index)
        {
        }

        public void Copy()
        {
        }

        public void Cut()
        {
        }

        public void Paste()
        {
        }

        public void Delete()
        {
        }

        public void SelectAll()
        {
        }

        #endregion
    }
}
