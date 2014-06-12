/*
 * Created by SharpDevelop.
 * User: Ihatenames
 * Date: 12.06.2014
 * Time: 16:29
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

using ExplorerTreeView;


namespace CEWSP
{
	/// <summary>
	/// Description of CewspTreeItemFactory.
	/// </summary>
	public class CewspTreeItemFactory : TreeItemFactory
	{
		public CewspTreeItemFactory()
		{
		}
		
		public CustomTreeItem CreateCustomTreeItemInstance()
		{
			return new CewspTreeViewItem();
		}
	}
}
