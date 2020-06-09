loc="/usr/local/install/send"

# Make sure the system is up to date for installation
# Not always necessary
#echo "Update"
#sudo apt-get update -y --force-yes -qq
#echo "Upgrade"
#sudo apt-get upgrade -y --force-yes -qq

echo "Installing EDS"
# Silent.ini answers the questions asked during the installation
sudo apt-get install -q -y $loc/installation_files/EdgeDataStore.deb < $loc/silent.ini

condition=0
echo
echo "Waiting"
# Wait for it to setup and start running
for (( ; ; ))
do
	if curl --fail -s http://localhost:5590/api/v1/configuration > /dev/null; then
		echo "Device: Get config succeeded, EDS is running!"
		break;
	else
		echo "Device: Get config failed, waiting 5 seconds to retry..."
		sleep 5
	fi
	((condition++))
	if ((condition > 10)); then 	
		echo "Device: Things didn't work..."
		exit 1
	else
		echo "Try $condition"
	fi
done

echo
echo "Configure system based on JSON files"
echo "Configure datasource"
# Update files
curl -i -d "@$loc/Modbus1Datasource.json" -H "Content-Type: application/json" -X PUT http://localhost:5590/api/v1/configuration/Modbus1/Datasource
echo
echo "Configure dataselection"
curl -i -d "@$loc/Modbus1Dataselection.json" -H "Content-Type: application/json" -X PUT http://localhost:5590/api/v1/configuration/Modbus1/Dataselection
echo
echo "Configure egress"
curl -i -d "@$loc/PeriodicEgressEndpoints.json" -H "Content-Type: application/json" -X PUT http://localhost:5590/api/v1/configuration/storage/PeriodicEgressEndpoints/
		
		