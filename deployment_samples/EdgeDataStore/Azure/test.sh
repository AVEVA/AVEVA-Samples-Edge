echo "Test: Read settings from config.ini..."
source <(grep = config.ini | tr -d "\r")

echo "Test: Installing Azure CLI IoT Extension..."
az extension add --name azure-cli-iot-ext

echo "Test: Logging in..."
az login --service-principal -u $Username -p $Password -t $Tenant

echo "Test: Set the active subscription..."
az account set --subscription $Subscription

echo "Test: Start the VM..."
az vm start -g $ResourceGroup -n $VmName

echo "Test: Set up the iotedge-config.json file..."
sed -i s/{azureContainerRegistryName}/$AcrName/g iotedge-config.json
sed -i s/{azureContainerRegistryAddress}/$AcrAddress/g iotedge-config.json
sed -i s/{azureContainerRegistryUsername}/$AcrUsername/g iotedge-config.json
sed -i s~{azureContainerRegistryPassword}~$AcrPassword~g iotedge-config.json
sed -i s~{azureContainerRegistryImageUri}~$AcrImageUri~g iotedge-config.json

echo "Test: Running remote deployment script..."
./remote.sh

echo "Test: Running reset script..."
./reset.sh

echo "Test: Stop the VM..."
az vm stop -g $ResourceGroup -n $VmName

echo "Test: Deallocate the VM..."
az vm deallocate -g $ResourceGroup -n $VmName

echo "Test: Complete!"