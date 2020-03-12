# Bash: Exit on error
set -e

echo "Remote: Read settings from config.ini..."
source <(grep = config.ini | tr -d "\r")

echo "Remote: Creating IoT Edge device in IoT Hub..."
az iot hub device-identity create --device-id $DeviceId --hub-name $HubName --edge-enabled

echo "Remote: Deploying modules to device..."
az iot edge set-modules --device-id $DeviceId --hub-name $HubName --content $IotEdgeConfigPath

echo "Remote: Retrieving connection string..."
ConnectionString=$(az iot hub device-identity show-connection-string --device-id $DeviceId --hub-name $HubName | sed -n 's/.*\(HostName=.*\)".*/\1/p')
ConnectionString=${ConnectionString//;/\\;}

echo "Remote: Preparing send folder..."
rm -rf ./send
mkdir -p ./send
cp -a ./device.sh ./send/device.sh
cp -a $EdsConfigPath ./send/config.json

echo "Remote: Creating a backup..."
rm -rf ./backup/$IPAddress
mkdir -p ./backup/$IPAddress
cp -r ./send/* ./backup/$IPAddress

echo "Remote: Sending files to device..."
ssh $UserId@$IPAddress "sudo rm -rf /usr/local/eds-install"
ssh $UserId@$IPAddress "sudo mkdir -m777 -p /usr/local/eds-install"
scp -r send/* $UserId@$IPAddress:/usr/local/eds-install

echo "Remote: Running edge device setup script..."
ssh $UserId@$IPAddress /usr/local/eds-install/device.sh $OS "$ConnectionString"

echo "Remote: Complete!"
