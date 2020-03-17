# Bash: Exit on error
# set -e

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
ssh -o "StrictHostKeyChecking=no" $UserId@$IpAddress
echo "Test: Copy ssh key to device..."
echo "$Password" | sshpass ssh-copy-id -f $UserId@$IpAddress

echo "Test: Running remote deployment script..."
./remote.sh

echo "Test: run remote.sh with config file"
cat loc.ini | ./remote.sh

echo "Test: See if files are there"
file1=`cat backup/location\=here/send/PeriodicEgressEndpoints.json`

if [[ $file1 == *"diagnostics"* ]] 
	then
		echo "Egress endpoint in right spot and configured"
	else
		exit 1
fi
  
echo "Test: See if files are there2"
file2=`cat send/PeriodicEgressEndpoints.json`
if [[ $file2 == *"diagnostics"* ]] 
	then
		echo "Egress endpoint in right spot and configured"
	else
		exit 1
fi


echo "Test: Running reset script..."
./reset.sh

echo "Test: Deallocate the VM..."
az vm deallocate -g $AzResourceGroup -n $AzVmName

echo "Test: Complete!"
