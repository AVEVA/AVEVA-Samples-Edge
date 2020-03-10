# OSIsoft Edge Data Store Install Deployment Sample

This sample uses bash scripts to install and configure Edge Data Store on a remote edge device.

## Requirements

### One-Time Local Setup

1. If on Windows, install Windows Subsystem for Linux, see [Microsoft Docs](https://docs.microsoft.com/en-us/windows/wsl/install-win10)  
   Powershell:
   ```powershell
   Enable-WindowsOptionalFeature -Online -FeatureName Microsoft-Windows-Subsystem-Linux
   ```
   Then install a Linux distribution like [Ubuntu](https://www.microsoft.com/store/apps/9N9TNGVNDL3Q), and use `bash` for all other commands in this ReadMe.
1. Prepare an `installation_files` folder with the Edge Data Store `.deb` files for the required processor architecture(s)

### Per-Device Setup

1. (Optional) Set up SSH for passwordless login, see [Linuxize Article](https://linuxize.com/post/how-to-setup-passwordless-ssh-login/)
   ```bash
   ssh-keygen -t rsa -b 4096 -C "your_email@domain.com"
   ssh-copy-id remote_username@device_ip_address
   ssh remote_username@device_ip_address
   ```
1. (Optional) Configure [loc.ini](loc.ini) with required information for the device
1. (Optional) Configure [egress.ini](egress.ini) with required data egress information for the device

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
   1. Install Edge Data Store silently
   1. Verify the installation succeeded
   1. Configure Edge Data Store egress

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
