/*
 * Created by SharpDevelop.
 * User: omggomb
 * Date: 12.06.2014
 * Time: 16:33
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using CEWSP;
using CEWSP.SourceFileTracking;
using CEWSP.Utils;
using ExplorerTreeView;

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
		
		/// <summary>
		/// Additionally marks an entry as tracked if need be
		/// </summary>
		/// <param name="oEntryImage"></param>
		public override void MakeHeader(System.Windows.Controls.Image oEntryImage)
		{
			base.MakeHeader(oEntryImage);
			
			string sRelPath = CPathUtils.ExtractRelativeToGameFolder(FullPathToReference);
			
			if (sRelPath == FullPathToReference)
				sRelPath = CPathUtils.ExtractRelativeToRoot(FullPathToReference);
			
			sRelPath += "\\";
			
			if (CSourceTracker.Instance.IsFileTracked(FullPathToReference))
			{
				MarkTracked();
			}
			
			if (IsDirectory && sRelPath != ".\\")
			{
				if (CSourceTracker.Instance.DoesDirectoryContainTrackedFile(sRelPath))
					MarkTracked(false);
			}
		}
		
		/// <summary>
		/// Adds [Tracked] to the DisplayName property of this entry
		/// </summary>
		/// <param name="bIncludeSubDirs">If true, marks all children of this item as tracked, too</param>
		public void MarkTracked(bool bIncludeSubDirs = true)
		{
			if (IsDirectory && bIncludeSubDirs)
			{
				foreach (CewspTreeViewItem element in Items)
				{
					element.MarkTracked();
				}
			}
			
			
			
			if (!DisplayName.Contains("[Tracked]"))
			{
				DisplayName = IdentificationName + " [Tracked]";
				MakeHeader(null);
			}
			
		}
		
		/// <summary>
		/// Removes [Tracked] from the item's display name
		/// </summary>
		/// <param name="bIncludeSubDirs">If true removes track indicator from all children, too</param>
		public void UnmarkTracked(bool bIncludeSubDirs = true)
		{
			if (IsDirectory && bIncludeSubDirs)
			{
				foreach (CewspTreeViewItem element in Items)
				{
					element.UnmarkTracked();
				}
			}
			
			
			if (DisplayName.Contains("[Tracked]"))
			{
				DisplayName = IdentificationName;
				MakeHeader(null);
			}	
		}
	}
}
