# OSIsoft Edge Data Store Azure IoT Deployment Sample

This sample uses bash scripts to deploy Edge Data Store using Azure IoT Hub and Azure IoT Edge Modules.

## Requirements

### One-Time Local Setup

1. If on Windows, install Windows Subsystem for Linux, see [Microsoft Docs](https://docs.microsoft.com/en-us/windows/wsl/install-win10)  
   Powershell:
   ```powershell
   Enable-WindowsOptionalFeature -Online -FeatureName Microsoft-Windows-Subsystem-Linux
   ```
   Then install a Linux distribution like [Ubuntu](https://www.microsoft.com/store/apps/9N9TNGVNDL3Q), and use `bash` for all other commands in this ReadMe.
1. Install Azure CLI, see [Microsoft Docs](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli-apt?view=azure-cli-latest#install-with-one-command)  
   **Note: if preferred, download script and inspect it before running**
   ```bash
   curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
   ```
1. Install IoT extension for Azure CLI, see [Microsoft Docs](https://github.com/Azure/azure-iot-cli-extension#installation)
   ```bash
   az extension add --name azure-cli-iot-ext
   ```
1. Install Docker, see [Docker Docs](https://docs.docker.com/install/linux/docker-ce/ubuntu/#install-using-the-convenience-script)  
   **Note: if preferred, download script and inspect it before running**
   ```bash
   curl -fsSL https://get.docker.com -o get-docker.sh
   sudo sh get-docker.sh
   ```
1. Create an Azure IoT Hub with IoT Edge enabled, see [Microsoft Docs](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-create-through-portal)
1. Create an Azure Container Registry with Admin user enabled, see [Microsoft Docs](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-get-started-portal)
1. Build Edge Data Store container matching the edge device processor architecture (ARM32, ARM64, or AMD64), see [OSIsoft Docs](https://osisoft.github.io/Edge-Data-Store-Docs/V1/Docker/EdgeDocker.html)
1. Push container image to Azure Container Registry, see [Microsoft Docs](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-get-started-portal#push-image-to-registry)
   ```bash
   docker push <acrLoginServer>/edgedatastore:v1
   ```
1. Configure specified IotEdgeConfigPath file ([iotedge-config.json](iotedge-config.json) by default) with required Azure IoT Edge Device Module information, specifically the required Azure Container Registry details, see [Microsoft Docs](https://osisoft.github.io/Edge-Data-Store-Docs/V1/Configuration/EdgeSystemConfiguration.html)
   1. {azureContainerRegistryAddress} should be the 'Login server'
   1. {azureContainerRegistryPassword} should be the 'password' from 'Access Keys'
   1. {azureContainerRegistryUsername} should be the 'Username' from 'Access Keys'
   1. {azureContainerRegistryImageUrl} should be the specific image URI, like `myregistry.azurecr.io/edgedatastore:v1`

### Per-Device Setup

1. (Optional) Set up SSH for passwordless login, see [Linuxize Article](https://linuxize.com/post/how-to-setup-passwordless-ssh-login/)
   ```bash
   ssh-keygen -t rsa -b 4096 -C "your_email@domain.com"
   ssh-copy-id remote_username@device_ip_address
   ssh remote_username@device_ip_address
   ```
1. Configure [config.ini](config.ini) with required information for the device
1. If necessary, update the IotEdgeConfigPath file with the correct Edge Data Store container image for the destination device processor architecture (ARM32, ARM64, or AMD64)
1. Configure specified EdsConfigPath file ([eds-config.json](eds-config.json) by default) with required Edge Data Store system configuration, see [OSIsoft Docs](https://osisoft.github.io/Edge-Data-Store-Docs/V1/Configuration/EdgeSystemConfiguration.html)  
   **Note: The default EDS configuration file is the minimum configuration from the [OSIsoft Docs](https://osisoft.github.io/Edge-Data-Store-Docs/V1/Configuration/EdgeSystemConfiguration.html#configure-minimum-edge-data-store), and includes no Periodic Egress Endpoints or Adapters**

## Sample Deployment Process

The sample will execute the following steps to deploy and configure Edge Data Store.

1. Create an Azure IoT Edge Device in Azure IoT Hub
1. Deploy the Edge Data Store Azure IoT Edge Module to the Azure IoT Edge Device
1. Retrieve the connection string for the Azure IoT Edge Device
1. Prepare a folder of files to send to the edge device
1. Back up that folder by IP address to record a record of what was sent to the device
1. Send the folder to the edge device
1. Run the `device.sh` script on the device, which will run the following steps
   1. Prepare for and install the Azure IoT Edge runtime and Azure IoT Security Daemon
   1. Configure Azure IoT Edge by setting the device connection string
   1. Restart Azure IoT Edge and wait for Edge Data Store to be deployed
   1. Configure Edge Data Store using the specified configuration file

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
