# SLC-AS-ChatOps-PTP

This repository contains an automation script solution with scripts that can be used to retrieve PTP information from your DataMiner system using the DataMiner Teams bot.

The following scrips are currently available:

- [PTP Info](#ptp-info)

## Pre-requisites

Kindly ensure that your DataMiner system and your Microsoft Teams adhere to the pre-requisites described in [DM Docs](https://docs.dataminer.services/user-guide/Cloud_Platform/TeamsBot/Microsoft_Teams_Chat_Integration.html#server-side-prerequisites).

## PTP Info

Automation script that returns the PTP info for the selected input.

### Configuration

Before you can successfully run the PTP Info script, a memory file will need to be created that is used to select what info you want to retrieve.
The memory file needs to be named "ChatOps_PTP_Info_Options" and requires the following entries:

| Position | Value | Description |
|--|--|--|
| 0 | GM | Show GrandMaster |
| 1 | Node Status | Get Node Status Overview |
| 2 | Alarms | Get Alarms |

### Show GrandMaster

![Grandmaster info request](/Documentation/Grandmaster_Info_Request.png)
![Grandmaster info response](/Documentation/Grandmaster_Info_Response.png)

### Get Node Status Overview

![Node status overview request](/Documentation/Get_Node_Status_Overview_Request.png)
![Node status overview response](/Documentation/Get_Node_Status_Overview_Response.png)

### Get Alarms

![Alarms request](/Documentation/Get_Alarms_Request.png)
![Alarms response](/Documentation/Get_Alarms_Response.png)
