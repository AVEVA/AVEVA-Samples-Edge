echo "Reset Device: Uninstall IoT Edge runtime..."
echo -e "y" | sudo apt remove osisoft.edgedatastore

echo "Reset Device: Remove OSIsoft directory..."
sudo rm -rf /usr/share/OSIsoft

echo "Reset Device: Complete!"
