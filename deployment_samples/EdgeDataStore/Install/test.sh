echo "Test: Read settings from config.ini..."
source <(grep = config.ini | tr -d "\r")

echo "Test: Logging in..."
az login --service-principal -u $AzUsername -p $AzPassword -t $AzTenant

echo "Test: Set the active subscription..."
az account set --subscription $AzSubscription

echo "Test: Start the VM..."
az vm start -g $AzResourceGroup -n $AzVmName

echo "Test: Install sshpass..."
sudo apt install sshpass

echo "Test: Generate key for passwordless login to device..."
echo "" | ssh-keygen -t rsa -b 4096 -C productreadiness -P ""
echo "Test: Disable Strict Host Key Checking..."
ssh -o "StrictHostKeyChecking=no" $UserName@$IpAddress
echo "Test: Copy ssh key to device..."
echo "exit" | echo "$Password" | sshpass ssh-copy-id -f $UserName@$IpAddress

echo "Test: Copy EDS install kit to agent machine"
scp -r $UserName@$IpAddress:/home/$UserName/EdgeDataStore_linux-x64.deb ./templates/installation_files/EdgeDataStore_linux-x64.deb

# Bash: Exit on error
set -e

echo "Test: run remote.sh with config file"
echo loc.ini | ./remote.sh
  
echo "Test: See if files are locally as expected"
file2=`cat send/PeriodicEgressEndpoints.json`
if [[ $file2 == *"diagnostics"* ]] 
	then
		echo "Egress endpoint in right spot and configured"
	else
		exit 1
fi

# Bash: No exit on error
set +e

echo "Test: Running reset script..."
./reset.sh

echo "Test: Deallocate the VM..."
az vm deallocate -g $AzResourceGroup -n $AzVmName

echo "Test: Complete!"