echo "Reset Device: Uninstall IoT Edge runtime"
echo -e "y" | sudo apt-get remove --purge iotedge

echo "Reset Device: Remove containers"
sudo docker rm -f $(sudo docker ps -a -q)

echo "Reset Device: Remove container images"
sudo docker rmi -f $(sudo docker images -q)

echo "Reset Device: Uninstall the container runtime"
echo -e "y" | sudo apt-get remove --purge moby-engine

# TODO Remove /usr/share/OSIsoft folder

echo "Reset Device: Complete!"
