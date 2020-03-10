echo "Device: Registering Microsoft key and software repository feed..."
curl https://packages.microsoft.com/config/$1/multiarch/prod.list > ./microsoft-prod.list
sudo cp ./microsoft-prod.list /etc/apt/sources.list.d/
curl https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.gpg
sudo cp ./microsoft.gpg /etc/apt/trusted.gpg.d/

echo "Device: Update..."
sudo apt-get update

echo "Device: Install the container runtime..."
echo -e "y" | sudo apt-get install moby-engine
echo -e "y" | sudo apt-get install moby-cli

echo "Device: Install the Azure IoT Security Daemon..."
echo -e "y" | sudo apt-get install iotedge

echo "Device: Set device connection string..."
sudo sed -i "s#\(device_connection_string: \).*#\1\"$2\"#g" /etc/iotedge/config.yaml

echo "Device: Restart iotedge in 5 seconds..."
sleep 5

echo "Device: Restart iotedge..."
sudo systemctl restart iotedge

echo "Device: Wait for EDS IoT Edge Module to start..."
for (( ; ; ))
do
  if curl --fail -s http://localhost:5590/api/v1/configuration > /dev/null; then
    echo "Device: Get config succeeded, EDS is running!"
    break;
  else
    echo "Device: Get config failed, waiting 5 seconds to retry..."
    sleep 5
  fi
done

echo "Device: Update EDS configuration..."
curl -i -d "@/usr/local/eds-install/config.json" -H "Content-Type: application/json" -X PUT http://localhost:5590/api/v1/configuration
