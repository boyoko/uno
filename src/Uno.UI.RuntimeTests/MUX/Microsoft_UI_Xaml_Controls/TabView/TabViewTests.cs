﻿#if !WINDOWS_UWP
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using MUXControlsTestApp.Utilities;
using System;
using System.Threading;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Common;
using System.Collections.Generic;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Provider;
using Private.Infrastructure;
using Microsoft.UI.Xaml.Controls;

#if USING_TAEF
using WEX.TestExecution;
using WEX.TestExecution.Markup;
using WEX.Logging.Interop;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
#endif

using Symbol = Windows.UI.Xaml.Controls.Symbol;

using SymbolIconSource = Microsoft.UI.Xaml.Controls.SymbolIconSource;

namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests
{

	[TestClass]
	public class TabViewTests
	{

		[TestMethod]
		public void VerifyCompactTabWidthVisualStates()
		{
			TabView tabView = null;
			RunOnUIThread.Execute(() =>
			{
				tabView = new TabView();
				TestServices.WindowHelper.WindowContent = tabView;

				tabView.TabItems.Add(CreateTabViewItem("Item 0", Symbol.Add));
				tabView.TabItems.Add(CreateTabViewItem("Item 1", Symbol.AddFriend));
				tabView.TabItems.Add(CreateTabViewItem("Item 2"));

				tabView.SelectedIndex = 0;
				tabView.SelectedItem = tabView.TabItems[0];
				(tabView.SelectedItem as TabViewItem).IsSelected = true;
				Verify.AreEqual("Item 0", (tabView.SelectedItem as TabViewItem).Header);
				//TestServices.WindowHelper.WindowContent.UpdateLayout();
			});
			// Waiting for layout
			TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				// Now set tab width mode
				tabView.TabWidthMode = TabViewWidthMode.Compact;
			});

			TestServices.WindowHelper.WaitForIdle();

			// Check if switching to compact updates all items correctly
			RunOnUIThread.Execute(() =>
			{
				VerifyTabWidthVisualStates(tabView.TabItems, true);
				tabView.TabItems.Add(CreateTabViewItem("Item 3"));
			});

			TestServices.WindowHelper.WaitForIdle();

			// Check if a newly added item has correct visual states
			RunOnUIThread.Execute(() =>
			{
				VerifyTabWidthVisualStates(tabView.TabItems, true);
				tabView.TabWidthMode = TabViewWidthMode.Equal;
			});

			TestServices.WindowHelper.WaitForIdle();

			// Switch back to non compact and check if every item has the correct visual state
			RunOnUIThread.Execute(() =>
			{
				VerifyTabWidthVisualStates(tabView.TabItems, false);
			});
		}

		[TestMethod]
		public void VerifyTabViewUIABehavior()
		{
			RunOnUIThread.Execute(() =>
			{
				TabView tabView = new TabView();
				TestServices.WindowHelper.WindowContent = tabView;

				tabView.TabItems.Add(CreateTabViewItem("Item 0", Symbol.Add));
				tabView.TabItems.Add(CreateTabViewItem("Item 1", Symbol.AddFriend));
				tabView.TabItems.Add(CreateTabViewItem("Item 2"));

				//Content.UpdateLayout();

				var tabViewPeer = FrameworkElementAutomationPeer.CreatePeerForElement(tabView);
				Verify.IsNotNull(tabViewPeer);
				var tabViewSelectionPattern = tabViewPeer.GetPattern(PatternInterface.Selection);
				Verify.IsNotNull(tabViewSelectionPattern);
				var selectionProvider = tabViewSelectionPattern as ISelectionProvider;
				// Tab controls must require selection
				Verify.IsTrue(selectionProvider.IsSelectionRequired);
			});
		}

		[TestMethod]
		public void VerifyTabViewItemUIABehavior()
		{
			TabView tabView = null;

			TabViewItem tvi0 = null;
			TabViewItem tvi1 = null;
			TabViewItem tvi2 = null;
			RunOnUIThread.Execute(() =>
			{
				tabView = new TabView();
				TestServices.WindowHelper.WindowContent = tabView;

				tvi0 = CreateTabViewItem("Item 0", Symbol.Add);
				tvi1 = CreateTabViewItem("Item 1", Symbol.AddFriend);
				tvi2 = CreateTabViewItem("Item 2");

				tabView.TabItems.Add(tvi0);
				tabView.TabItems.Add(tvi1);
				tabView.TabItems.Add(tvi2);

				tabView.SelectedIndex = 0;
				tabView.SelectedItem = tvi0;
				//Content.UpdateLayout();
			});

			TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				var selectionItemProvider = GetProviderFromTVI(tvi0);
				Verify.IsTrue(selectionItemProvider.IsSelected, "Item should be selected");

				selectionItemProvider = GetProviderFromTVI(tvi1);
				Verify.IsFalse(selectionItemProvider.IsSelected, "Item should not be selected");

				Log.Comment("Change selection through automationpeer");
				selectionItemProvider.Select();
				Verify.IsTrue(selectionItemProvider.IsSelected, "Item should have been selected");

				selectionItemProvider = GetProviderFromTVI(tvi0);
				Verify.IsFalse(selectionItemProvider.IsSelected, "Item should not be selected anymore");

				Verify.IsNotNull(selectionItemProvider.SelectionContainer);
			});

			static ISelectionItemProvider GetProviderFromTVI(TabViewItem item)
			{
				var peer = FrameworkElementAutomationPeer.CreatePeerForElement(item);
				var provider = peer.GetPattern(PatternInterface.SelectionItem)
								as ISelectionItemProvider;
				Verify.IsNotNull(provider);
				return provider;
			}
		}

		private static void VerifyTabWidthVisualStates(IList<object> items, bool isCompact)
		{
			foreach (var item in items)
			{
				var tabItem = item as TabViewItem;
				var rootGrid = VisualTreeHelper.GetChild(tabItem, 0) as FrameworkElement;

				foreach (var group in VisualStateManager.GetVisualStateGroups(rootGrid))
				{
					if (group.Name == "TabWidthModes")
					{
						if (tabItem.IsSelected || !isCompact)
						{
							Verify.AreEqual("StandardWidth", group.CurrentState.Name, "Verify that this tab item is rendering in standard width");
						}
						else
						{
							Verify.AreEqual("Compact", group.CurrentState.Name, "Verify that this tab item is rendering in compact width");
						}
					}
				}

			}
		}

		private static TabViewItem CreateTabViewItem(string name, Symbol icon, bool closable = true, bool enabled = true)
		{
			var tabViewItem = new TabViewItem();

			tabViewItem.Header = name;
			tabViewItem.IconSource = new SymbolIconSource() { Symbol = icon };
			tabViewItem.IsClosable = closable;
			tabViewItem.IsEnabled = enabled;

			return tabViewItem;
		}

		private static TabViewItem CreateTabViewItem(string name, bool closable = true, bool enabled = true)
		{
			var tabViewItem = new TabViewItem();

			tabViewItem.Header = name;
			tabViewItem.IsClosable = closable;
			tabViewItem.IsEnabled = enabled;

			return tabViewItem;
		}
	}
}
#endif