﻿/*
 * Original author: Nicholas Shulman <nicksh .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2011 University of Washington - Seattle, WA
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System.Linq;
using System.Collections;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Forms;
using pwiz.Common.DataBinding.Internal;

namespace pwiz.Common.DataBinding.Controls
{
    public class BindingListSource : BindingSource
    {
        public BindingListSource(IContainer container) : this(TaskScheduler.FromCurrentSynchronizationContext())
        {
            container.Add(this);
        }
        public BindingListSource() : this((TaskScheduler) null)
        {
        }

        private BindingListSource(TaskScheduler taskScheduler)
        {
            base.DataSource = BindingListView = new BindingListView(taskScheduler);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                BindingListView.Dispose();
            }
            base.Dispose(disposing);
        }

        internal BindingListView BindingListView { get; private set; }
        public IEnumerable RowSource
        {
            get { return BindingListView.RowSource; }
            set { BindingListView.RowSource = value; }
        }

        [Browsable(false)]
        public new object DataSource
        {
            get { return base.DataSource; }
        }

        public IViewContext ViewContext { get; private set; }
        public void SetViewContext(IViewContext viewContext, ViewInfo viewInfo)
        {
            ViewContext = viewContext;
            BindingListView.ViewInfo = viewInfo;
            OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
        }
        public void SetViewContext(IViewContext viewContext)
        {
            ViewInfo viewInfo = BindingListView.ViewInfo;
            if (viewInfo == null && viewContext != null)
            {
                var viewSpec = viewContext.BuiltInViewSpecs.FirstOrDefault();
                if (viewSpec != null)
                {
                    viewInfo = new ViewInfo(viewContext.ParentColumn, viewSpec);
                }
            }
            SetViewContext(viewContext, viewInfo);
        }
        public ViewInfo ViewInfo
        {
            get { return BindingListView.ViewInfo; }
            set { BindingListView.ViewInfo = value; }
        }

        public ViewSpec ViewSpec
        {
            get { return ViewInfo.GetViewSpec(); }
        }
    }
}
