/*
****************************************************************************
*  Copyright (c) 2023,  Skyline Communications NV  All Rights Reserved.    *
****************************************************************************

By using this script, you expressly agree with the usage terms and
conditions set out below.
This script and all related materials are protected by copyrights and
other intellectual property rights that exclusively belong
to Skyline Communications.

A user license granted for this script is strictly for personal use only.
This script may not be used in any way by anyone without the prior
written consent of Skyline Communications. Any sublicensing of this
script is forbidden.

Any modifications to this script by the user are only allowed for
personal use and within the intended purpose of the script,
and will remain the sole responsibility of the user.
Skyline Communications will not be responsible for any damages or
malfunctions whatsoever of the script resulting from a modification
or adaptation by the user.

The content of this script is confidential information.
The user hereby agrees to keep this confidential information strictly
secret and confidential and not to disclose or reveal it, in whole
or in part, directly or indirectly to any person, entity, organization
or administration without the prior written consent of
Skyline Communications.

Any inquiries can be addressed to:

	Skyline Communications NV
	Ambachtenstraat 33
	B-8870 Izegem
	Belgium
	Tel.	: +32 51 31 35 69
	Fax.	: +32 51 31 01 29
	E-mail	: info@skyline.be
	Web		: www.skyline.be
	Contact	: Ben Vandenberghe

****************************************************************************
Revision History:

DATE		VERSION		AUTHOR			COMMENTS

27/01/2023	1.0.0.1		JVW, Skyline	Initial version
****************************************************************************
*/

namespace Script
{
	using System;
	using System.Collections.Generic;

	using AdaptiveCards;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Core.DataMinerSystem.Automation;
	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Net.Messages;

	/// <summary>
	/// DataMiner Script Class.
	/// </summary>
	public class Script
	{
		private IEngine engine;

		/// <summary>
		/// The Script entry point.
		/// </summary>
		/// <param name="engine">Link with SLAutomation process.</param>
		public void Run(Engine engine)
		{
			this.engine = engine;

			try
			{
				RunSafe();
			}
			catch (Exception ex)
			{
				engine.GenerateInformation(ex.Message);
				engine.AddScriptOutput("ERROR", ex.Message);
			}
		}

		private static string TranslateClockSource(int? value)
		{
			switch (value)
			{
				case 0: return "Not Available";
				case 16: return "Atomic Clock";
				case 32: return "GPS";
				case 48: return "Terrestrial Radio";
				case 64: return "PTP";
				case 80: return "NTP";
				case 96: return "Hand Set";
				case 144: return "Other";
				case 160: return "Internal Oscillator";

				default: return "Unknown";
			}
		}

		private static string TranslateRole(int value)
		{
			switch (value)
			{
				case 1: return "Grandmaster";
				case 2: return "Transparent Clock";
				case 3: return "Boundary Clock";
				case 4: return "Slave";

				default: return "Unknown";
			}
		}

		private static string TranslateStatus(int value)
		{
			switch (value)
			{
				case 1: return "Synced With Preferred Grandmaster";
				case 2: return "Synced With Non-Preferred Grandmaster";
				case 3: return "Synced With Other Device";
				case 4: return "Unknown";
				case 5: return "Synced With Active Preferred Grandmaster";

				default: return "Unknown";
			}
		}

		private static AdaptiveContainerStyle TranslateStatusSeverity(Skyline.DataMiner.Net.Messages.AlarmLevel alarmLevel)
		{
			switch (alarmLevel)
			{
				case Skyline.DataMiner.Net.Messages.AlarmLevel.Undefined:
				case Skyline.DataMiner.Net.Messages.AlarmLevel.Information:
				case Skyline.DataMiner.Net.Messages.AlarmLevel.Timeout:
				case Skyline.DataMiner.Net.Messages.AlarmLevel.Initial:
				case Skyline.DataMiner.Net.Messages.AlarmLevel.Masked:
				case Skyline.DataMiner.Net.Messages.AlarmLevel.Error:
				case Skyline.DataMiner.Net.Messages.AlarmLevel.Notice:
				case Skyline.DataMiner.Net.Messages.AlarmLevel.Suggestion:
					return AdaptiveContainerStyle.Default;

				case Skyline.DataMiner.Net.Messages.AlarmLevel.Normal:
					return AdaptiveContainerStyle.Good;

				case Skyline.DataMiner.Net.Messages.AlarmLevel.Warning:
					return AdaptiveContainerStyle.Warning;

				case Skyline.DataMiner.Net.Messages.AlarmLevel.Minor:
				case Skyline.DataMiner.Net.Messages.AlarmLevel.Major:
					return AdaptiveContainerStyle.Emphasis;

				case Skyline.DataMiner.Net.Messages.AlarmLevel.Critical:
					return AdaptiveContainerStyle.Attention;

				default:
					return AdaptiveContainerStyle.Default;
			}
		}

		private void RunSafe()
		{
			var request = engine.GetScriptParam("Request").Value;

			switch (request)
			{
				case "GM":
					GetGrandMasterInfo();
					break;

				case "Node Status":
					GetNodeStatus();
					break;

				case "Alarms":
					GetAlarms();
					break;

				default:
					throw new NotSupportedException($"Request '{request}' is not supported.");
			}
		}

		private void GetGrandMasterInfo()
		{
			IDms dms = engine.GetDms();

			IDmsElement ptpElement = dms.GetElement("DataMiner PTP");
			var grandMasterElementInfo = ptpElement.GetStandaloneParameter<string>(1210).GetValue();

			IDmsElement grandMasterElement = dms.GetElement(new DmsElementId(grandMasterElementInfo));
			var clockSource = grandMasterElement.GetStandaloneParameter<int?>(74058).GetValue();
			var id = grandMasterElement.GetStandaloneParameter<string>(74002).GetValue();

			var adaptiveCardBody = new List<AdaptiveElement>
			{
				new AdaptiveTextBlock("Grandmaster Info")
				{
					Type = "TextBlock",
					Weight = AdaptiveTextWeight.Bolder,
					Size = AdaptiveTextSize.Large,
				},
				new AdaptiveFactSet
				{
					Type = "FactSet",
					Facts = new List<AdaptiveFact>
					{
						new AdaptiveFact("Name:", grandMasterElement.Name),
						new AdaptiveFact("Clock Source:", TranslateClockSource(clockSource)),
						new AdaptiveFact("ID:", id),
					},
				},
			};

			engine.AddScriptOutput("AdaptiveCard", JsonConvert.SerializeObject(adaptiveCardBody));
		}

		private void GetNodeStatus()
		{
			IDms dms = engine.GetDms();
			IDmsElement ptpElement = dms.GetElement("DataMiner PTP");

			GetPartialTableMessage gptm = new GetPartialTableMessage
			{
				DataMinerID = ptpElement.DmsElementId.AgentId,
				ElementID = ptpElement.DmsElementId.ElementId,
				ParameterID = 1000,
				Filters = new[] { "columns=1004,1011" },
			};

			ParameterChangeEventMessage pcem = (ParameterChangeEventMessage)Engine.SLNet.SendSingleResponseMessage(gptm);

			var nodeInfoByRowPosition = new Dictionary<int, NodeInfo>();
			for (int iColumn = 0; iColumn < pcem.NewValue.ArrayValue.Length; iColumn++)
			{
				var column = pcem.NewValue.ArrayValue[iColumn];

				for (int iRow = 0; iRow < column.ArrayValue.Length; iRow++)
				{
					if (!nodeInfoByRowPosition.TryGetValue(iRow, out NodeInfo nodeInfo))
					{
						nodeInfo = new NodeInfo();

						nodeInfoByRowPosition.Add(iRow, nodeInfo);
					}

					var row = column.ArrayValue[iRow];

					if (iColumn == 0)
					{
						nodeInfo.ElementInfo = row.CellValue.StringValue;
						nodeInfo.Name = row.CellDisplayKey;
					}
					else if (iColumn == 1)
					{
						nodeInfo.Role = row.CellValue.Int32Value;
					}
					else
					{
						nodeInfo.Status = row.CellValue.Int32Value;
						nodeInfo.StatusAlarmLevel = row.CellActualAlarmLevel;
					}
				}
			}

			var table = new AdaptiveTable
			{
				Type = "Table",
				FirstRowAsHeaders = true,
				Columns = new List<AdaptiveTableColumnDefinition>
				{
					new AdaptiveTableColumnDefinition
					{
						Width = 150,
					},
					new AdaptiveTableColumnDefinition
					{
						Width = 100,
					},
					new AdaptiveTableColumnDefinition
					{
						Width = 250,
					},
				},
				Rows = new List<AdaptiveTableRow>
				{
					new AdaptiveTableRow
					{
						Type = "TableRow",
						Cells = new List<AdaptiveTableCell>
						{
							new AdaptiveTableCell
							{
								Type = "TableCell",
								Items = new List<AdaptiveElement>
								{
									new AdaptiveTextBlock("Name")
									{
										Type = "TextBlock",
										Weight = AdaptiveTextWeight.Bolder,
									},
								},
							},
							new AdaptiveTableCell
							{
								Type = "TableCell",
								Items = new List<AdaptiveElement>
								{
									new AdaptiveTextBlock("Role")
									{
										Type = "TextBlock",
										Weight = AdaptiveTextWeight.Bolder,
									},
								},
							},
							new AdaptiveTableCell
							{
								Type = "TableCell",
								Items = new List<AdaptiveElement>
								{
									new AdaptiveTextBlock("Status")
									{
										Type = "TextBlock",
										Weight = AdaptiveTextWeight.Bolder,
									},
								},
							},
						},
					},
				},
			};

			foreach (var nodeInfo in nodeInfoByRowPosition.Values)
			{
				var row = new AdaptiveTableRow
				{
					Type = "TableRow",
					Cells = new List<AdaptiveTableCell>
					{
						new AdaptiveTableCell
						{
							Type = "TableCell",
							Items = new List<AdaptiveElement>
							{
								new AdaptiveTextBlock(nodeInfo.Name)
								{
									Type = "TextBlock",
								},
							},
						},
						new AdaptiveTableCell
						{
							Type = "TableCell",
							Items = new List<AdaptiveElement>
							{
								new AdaptiveTextBlock(TranslateRole(nodeInfo.Role))
								{
									Type = "TextBlock",
								},
							},
						},
						new AdaptiveTableCell
						{
							Type = "TableCell",
							Items = new List<AdaptiveElement>
							{
								new AdaptiveTextBlock(TranslateStatus(nodeInfo.Status))
								{
									Type = "TextBlock",
								},
							},
							Style = TranslateStatusSeverity(nodeInfo.StatusAlarmLevel),
						},
					},
				};

				table.Rows.Add(row);
			}

			var adaptiveCardBody = new List<AdaptiveElement>();
			adaptiveCardBody.Add(table);

			engine.AddScriptOutput("AdaptiveCard", JsonConvert.SerializeObject(adaptiveCardBody));
		}

		private void GetAlarms()
		{
			GetAlarmFilterMessage gefm = new GetAlarmFilterMessage
			{
				Key = "ptp_alarms (shared filter)",
			};

			GetAlarmFilterResponse gafr = (GetAlarmFilterResponse)Engine.SLNet.SendSingleResponseMessage(gefm);

			GetActiveAlarmsMessage gaam = new GetActiveAlarmsMessage
			{
				Filter = gafr.Filter,
			};

			ActiveAlarmsResponseMessage responses = (ActiveAlarmsResponseMessage)Engine.SLNet.SendSingleResponseMessage(gaam);

			var adaptiveCardBody = new List<AdaptiveElement>();
			foreach (var alarm in responses.ActiveAlarms)
			{
				var value = string.Empty;
				switch (alarm.ParameterName)
				{
					case "Priority 1":
					case "PTP Domain":
						value = string.Format("{0:G29}", decimal.Parse(alarm.Value));
						break;

					default:
						value = alarm.Value;
						break;
				}

				var factSet = new AdaptiveFactSet
				{
					Type = "FactSet",
					Facts = new List<AdaptiveFact>
					{
						new AdaptiveFact("Element Name:", alarm.ElementName),
						new AdaptiveFact("Description:", alarm.ParameterName),
						new AdaptiveFact("Value:", value),
						new AdaptiveFact("Root Time:", alarm.RootTime.ToString()),
					},
				};

				var container = new AdaptiveContainer();
				container.Items.Add(factSet);

				switch (alarm.Severity)
				{
					case "Warning":
						container.Style = AdaptiveContainerStyle.Warning;
						break;

					case "Minor":
					case "Major":
						container.Style = AdaptiveContainerStyle.Emphasis;
						break;

					case "Critical":
						container.Style = AdaptiveContainerStyle.Attention;
						break;

					default:
						container.Style = AdaptiveContainerStyle.Good;
						break;
				}

				adaptiveCardBody.Add(container);
			}

			engine.AddScriptOutput("AdaptiveCard", JsonConvert.SerializeObject(adaptiveCardBody));
		}

		private sealed class NodeInfo
		{
			public string ElementInfo { get; set; }

			public string Name { get; set; }

			public int Role { get; set; }

			public int Status { get; set; }

			public Skyline.DataMiner.Net.Messages.AlarmLevel StatusAlarmLevel { get; set; }
		}
	}
}