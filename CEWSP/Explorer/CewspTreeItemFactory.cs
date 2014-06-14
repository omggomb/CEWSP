/*
 * Created by SharpDevelop.
 * User: omggomb
 * Date: 12.06.2014
 * Time: 16:29
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Windows;
using System.Windows.Media.Imaging;

using ExplorerTreeView;

namespace CEWSP
{
	/// <summary>
	/// Description of CewspTreeItemFactory.
	/// </summary>
	public class CewspTreeItemFactory : TreeItemFactory
	{		
		public CustomTreeItem CreateCustomTreeItemInstance()
		{
			return new CewspTreeViewItem();
		}
		
		/// <summary>
		/// We use a custom made, yellowing folder icon here.
		/// </summary>
		/// <param name="itemThatIsUsed"></param>
		/// <returns></returns>
		public System.Windows.Controls.Image CreateFolderIconImage(CustomTreeItem itemThatIsUsed)
		{
			var img = new System.Windows.Controls.Image();
			
			var src = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(Properties.Icons.SimpleFolderIcon.Handle,
			                                                                 Int32Rect.Empty,
			                                                                 BitmapSizeOptions.FromEmptyOptions());
			
			img.Source = src;
			return img;
		}
	}
}
