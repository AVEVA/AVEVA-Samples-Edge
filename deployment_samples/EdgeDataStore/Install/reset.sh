echo "Reset: Read settings from config.ini..."
source <(grep = config.ini | tr -d "\r")

echo "Reset: Send device reset script to device..."
echo scp -r reset-device.sh $UserName@$IpAddress:/usr/local/install
scp -r reset-device.sh $UserName@$IpAddress:/usr/local/install

echo "Reset: Running device reset script..."
ssh $UserName@$IpAddress /usr/local/install/reset-device.sh

echo "Reset: Complete!"
