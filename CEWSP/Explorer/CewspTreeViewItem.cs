/*
 * Created by SharpDevelop.
 * User: Ihatenames
 * Date: 12.06.2014
 * Time: 16:33
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

using ExplorerTreeView;

using CEWSP;
using CEWSP.SourceFileTracking;

namespace CEWSP
{
	/// <summary>
	/// Description of CewspTreeViewItem.
	/// </summary>
	public class CewspTreeViewItem : CustomTreeItem
	{
		public CewspTreeViewItem() : base()
		{
		
		}
		
		public override void MakeHeader(System.Windows.Controls.Image oEntryImage)
		{
			base.MakeHeader(oEntryImage);
			
			if (CSourceTracker.Instance.IsFileTracked(FullPathToReference))
				ExplorerContextMenu.MarkAsTracked(this);
		}
		
	}
}
