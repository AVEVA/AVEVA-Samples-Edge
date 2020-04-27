# OSIsoft Edge Data Store Install Deployment Sample

**Version:** 1.0.0

[![Build Status](https://dev.azure.com/osieng/engineering/_apis/build/status/product-readiness/OCS/CSVtoOCS_DotNet?branchName=master)](https://dev.azure.com/osieng/engineering/_build/latest?definitionId=1393&branchName=master)

This sample uses bash scripts to install and configure Edge Data Store on a remote Linux edge device.

## Requirements

### One-Time Local Setup

1. If on Windows, install Windows Subsystem for Linux, see [Microsoft Docs](https://docs.microsoft.com/en-us/windows/wsl/install-win10)  
   Powershell:

   ```powershell
   Enable-WindowsOptionalFeature -Online -FeatureName Microsoft-Windows-Subsystem-Linux
   ```

   Then install a Linux distribution like [Ubuntu](https://www.microsoft.com/store/apps/9N9TNGVNDL3Q), and use `bash` for all other commands in this ReadMe.

1. Prepare an `installation_files` folder with the Edge Data Store `.deb` files for the required processor architecture(s)
1. (Optional) Configure template files in ./templates as desired

### Per-Device Setup

1. (Optional) Set up SSH for passwordless login, see [Linuxize Article](https://linuxize.com/post/how-to-setup-passwordless-ssh-login/)

   ```bash
   ssh-keygen -t rsa -b 4096 -C "your_email@domain.com"
   ssh-copy-id remote_username@device_ip_address
   ssh remote_username@device_ip_address
   ```

1. (Optional) Configure [loc.ini](loc.ini) with required information for the device
1. (Optional) Configure [egress.ini](egress.ini) with required data egress information for the device
1. (Optional) Configure template files in `./templates` as desired

The default template files include egress at a 10 second interval, as well as an example Modbus adapter configuration. The Modbus adapter is not expected to function without modification as it does not represent a real Modbus device.

To disable deployment and configuration of the Modbus adapter, change the 'y' to 'N' in the the [silent.ini](./templates/silent.ini) file and comment out the lines in [script.sh](./templates/script.sh) that run `curl` requests to configure the `Modbus1` endpoint.

## Sample Deployment Process

The sample will execute the following steps to install and configure Edge Data Store.

1. Copy configuration templates so originals are not modified
1. Prompt for a location file, or request required information at runtime
1. Prompt for Edge Data Store egress information, from file or at runtime
1. Write egress configuration to file
1. Prepare a folder of files to send to the edge device
1. Clean up local copies of modified template files
1. Back up the send folder by IP address to record a record of what was sent to the device
1. Send the folder to the edge device
1. Run the `script.sh` script on the device, which will run the following steps
   1. Install Edge Data Store silently and create a Modbus adapter instance
   1. Verify the installation succeeded
   1. Configure Edge Data Store Modbus Adapter Data Source
   1. Configure Edge Data Store Modbus Adapter Data Selection
   1. Configure Edge Data Store Periodic Egress

## Running the Sample

Open a bash terminal, and run the `remote.sh` script

```bash
./remote.sh
```

---

For the Edge Data Store deployment landing page [ReadMe](../)  
For the Edge deployment landing page [ReadMe](../../)  
For the Edge landing page [ReadMe](../../../)  
For the OSIsoft Samples landing page [ReadMe](https://github.com/osisoft/OSI-Samples)
