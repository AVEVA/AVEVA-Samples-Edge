# Bash: Exit on error
set -e

echo "Test: Read settings from config.ini..."
source <(grep = config.ini | tr -d "\r")

echo "Test: Installing Azure CLI..."
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash

echo "Test: Installing Azure CLI IoT Extension..."
az extension add --name azure-cli-iot-ext

echo "Test: Logging in..."
az login --service-principal -u $Username -p $Password -t $Tenant

echo "Test: Set the active subscription..."
az account set --subscription $Subscription

echo "Test: Start the VM"
az vm start -g $ResourceGroup -n $VmName

echo "Test: Running remote deployment script..."
./remote.sh

echo "Test: Running reset script..."
./reset.sh

echo "Test: Stop the VM"
az vm stop -g $ResourceGroup -n $VmName

echo "Test: Complete!"