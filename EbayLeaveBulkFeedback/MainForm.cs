﻿using EbayLeaveBulkFeedback;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EbayLeaveBulkFeedback
{
	public partial class MainForm : Form
	{
		private DataManager _dataManager;
		Thread _leaveFeedbackThread;
		private ItemPickDialog _itemPickDialog;
		public ItemPickDialog ItemPickDialog
		{
			get
			{
				if (_itemPickDialog == null)
				{
					_itemPickDialog = new ItemPickDialog(_dataManager)
					{
						PickItemsAction = AddRawItem
					};
				}

				return _itemPickDialog;
			}
		}
		private ConfigDialog _configDialog;
		public ConfigDialog ConfigDialog
		{
			get
			{
				if (_configDialog == null || _configDialog.IsDisposed)
				{
					_configDialog = new ConfigDialog(_dataManager);
				}

				return _configDialog;
			}
		}

		public MainForm(DataManager dataManager)
		{
			InitializeComponent();
			_dataManager = dataManager;
			_dataManager.MainForm = this;
			_dataManager.FeedbackListViewChanged = UpdateItemCount;
			_dataManager.TextBoxRawFeedbackData = textBoxRawData;
			_dataManager.FeedbackListView = feedbackListView;
		}

		private void MainForm_Load(object sender, EventArgs e)
		{
			ResetGui();
			_dataManager.UpdateFeedbackListViewAsync();
		}

		private void ResetGui()
		{
			Invoke((MethodInvoker)(() =>
			{
				EnableDisableAll(true);
				GeneralStatusUpdate("Ready", 0);
			}));
		}

		private void textBoxRawData_TextChanged(object sender, EventArgs e)
		{
			_dataManager.UpdateFeedbackListViewAsync();
		}

		private void AddRawItem(string itemInfo)
		{
			textBoxRawData.Text += "\r\n" + itemInfo;
		}

		private void SanitizeList()
		{
			StringBuilder sb = new StringBuilder();
			int i = 0;
			foreach (ListViewItem listViewItem in feedbackListView.Items)
			{
				sb.Append((i++ == 0 ? string.Empty : " ") + listViewItem.SubItems[1].Text);
			}

			textBoxRawData.Text = sb.ToString();
		}

		private void buttonLeaveFeedback_Click(object sender, EventArgs e)
		{
			_leaveFeedbackThread = _dataManager.LeaveFeedbacksAsync(() => { EnableDisableAll(false); },
				ResetGui,
				GeneralStatusUpdate);
		}

		private void EnableDisableAll(bool enabled)
		{
			Invoke((MethodInvoker)(() =>
			{
				textBoxRawData.Enabled = enabled;
				buttonLeaveFeedback.Enabled = enabled;
				buttonStop.Enabled = !enabled;
			}));
		}

		private void GeneralStatusUpdate(string status, int? percentComplete)
		{
			Invoke((MethodInvoker)(() =>
			{
				if (status != null)
					((ToolStripStatusLabel)(statusStrip.Items["toolStripStatusLabel"])).Text = status;

				if (percentComplete.HasValue)
					((ToolStripProgressBar)(statusStrip.Items["toolStripProgressBar"])).Value = percentComplete.Value;
			}));
		}
		/*
		private void FeedbackUpdate(string itemId, FeedbackUpdates updates)
		{
			_dataManager.FeedbackUpdate(itemId, updates);
		}
		*/
		private void buttonStop_Click(object sender, EventArgs e)
		{
			if (_leaveFeedbackThread != null && _leaveFeedbackThread.IsAlive)
			{
				_leaveFeedbackThread.Abort();
			}

			ResetGui();
		}

		private void buttonSanitizeList_Click(object sender, EventArgs e)
		{
			SanitizeList();
		}

		private void buttonClearCompleted_Click(object sender, EventArgs e)
		{
			foreach (ListViewItem listViewItem in feedbackListView.Items)
			{
				string status = listViewItem.SubItems[0].Text;
				if (status == "Done" || status == "Ignore")
					Invoke((MethodInvoker)(() => { feedbackListView.Items.Remove(listViewItem); }));
			}

			SanitizeList();
		}

		private void buttonItemPicker_Click(object sender, EventArgs e)
		{
			ShowItemPicker();
		}

		private void ShowItemPicker()
		{
			ItemPickDialog.WindowState = FormWindowState.Maximized;
			ItemPickDialog.Show();
			ItemPickDialog.BringToFront();
		}

		private void UpdateItemCount()
		{
			toolStripItemCount.Text = "Items: " + feedbackListView.Items.Count.ToString();
		}

		private void buttonIgnoreListed_Click(object sender, EventArgs e)
		{
			var updates = new FeedbackUpdates()
			{
				Status = "Ignore"
			};

			foreach (ListViewItem item in feedbackListView.Items)
			{
				if (!string.IsNullOrEmpty(item.SubItems[0].Text))
					continue;	// don't update if it already has a status
				_dataManager.FeedbackUpdate(item.SubItems[1].Text, updates);
			}
		}

		private void buttonConfig_Click(object sender, EventArgs e)
		{
			ConfigDialog.Show();
			ConfigDialog.BringToFront();
		}
	}
}
