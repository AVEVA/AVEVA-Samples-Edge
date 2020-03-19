# Initialize file names to strings
egressFile="PeriodicEgressEndpoints.json"
silentFile="silent.ini"
OCSEgress="OCSEgress.json"
PIEgress="PIEgress.json"
dataSourceModbus="Modbus1Datasource.json"
dataSelectionModbus="Modbus1Dataselection.json"
	
# Copy files locally, to ensure the templates stay as templates	
cp -a ./templates/$dataSourceModbus ./$dataSourceModbus
cp -a ./templates/$dataSelectionModbus ./$dataSelectionModbus
cp -a ./templates/$OCSEgress ./$OCSEgress
cp -a ./templates/$PIEgress ./$PIEgress
cp -a ./templates/$silentFile ./$silentFile

# Prompt the user for an location file.  
# This allows you to have your location and username and OS information stored somewhere and look it up
read -p "Do you have an location file configured? (enter file name)  " locationFile	

# If location file is provided then use it 
if [ ! -z "$locationFile" ]
	then	
	# A location file assumes that the file is using linux line endings and the entries are as detailed below
		source <(grep = $locationFile | tr -d "\r")
fi
	
# If missing parameter or no file entered ask for individual things	
if [ -z "$location" ]
	then	
		read -p "Where are we installing EDS? (IP Address) " location	
fi

if [ -z "$userName" ]
	then	
		read -p "Remote Computer User ID?  " userName	
fi

if [ -z "$osType" ]
	then	
		read -p "What OS type are we sending to?  Linux X64 =1 or x64; Linux Arm 32 =anything else?  " osType	
fi
	
# Prompt the user for egress information
# In this configuration we can have multiple egress configured, each one different
egressBody=""
if [ -z "$egressToConfigure" ]
	then	
		read -p "How many egresses should we configure?  " egressToConfigure
fi
			
number="$egressToConfigure"
for (( i=1; i<=number; i++ ))
do
	# There maybe shared information across multiple EDSs or egress endpoints, so to ease interaction we define an egress.txt file
	# For each egress we need to substitute out the placeholders for enterable values
	# Some settings may be shared across egresses, so make these easily reused
	read -p "Do you have an egress configuration file? (enter file name)  " egressConfigFile
	
	if [ ! -z "$egressConfigFile" ]
		then	
	# An egress file assumes that the file is using linux line endings and the entries are as detailed below
			source <(grep = $egressConfigFile | tr -d "\r")
	fi

	# If missing parameter or no file entered ask for individual things				
	if [ -z "$egressType" ]
		then	
			read -p "Is this egress to PI? (y or pi) - all other responses go to OCS  " egressType
	fi
	
	if [ -z "$id" ]
		then	
			read -p "What is the UserName/ClientID?  " id	
	fi
	
	if [ -z "$egressPassword" ]
		then	
			read -p "What is the Password/Secret?  " egressPassword
	fi
	
	if [ -z "$url" ]
		then	
			read -p "What is the url? " url			
	fi		
	
	if [ -z "$locPrefix" ]
		then	
			if [ -z "$prefix" ]
				then	
					read -p "What is egress prefix?  " prefix	
			fi
		else			
			prefix=$locPrefix
	fi
	
	if [ -z "$namespaceInt" ]
		then	
			read -p "Which namespace? 1 or default=default all else =diagnostics  " namespaceInt		
	fi 
	
	if [ "$namespaceInt" == "1" ] || [ "$namespaceInt" == "default" ]
		then
			namespace="default"
		else
			namespace="diagnostics"	
			prefix+="diagnostics."
	fi		
			
	if [ "$egressType" == "y" ]  || [ "$egressType" == "pi" ]
		then
		# We are configuring PI		
			egress=$(<$PIEgress)
			egressID="pi.${namespace}"
		else
			egress=$(<$OCSEgress)	
			egressID="ocs.${namespace}"
	fi
	
	egress="${egress/<egressID>/$egressID}"  
	egress="${egress/<id>/$id}"  
	egress="${egress/<password>/$egressPassword}"  
	egress="${egress/<url>/$url}"  
	egress="${egress/<namespace>/$namespace}"  
	egress="${egress//<prefix>/$prefix}"  	
	egressBody+=$egress	
	unset egressConfigFile
	unset id
	unset egressPassword
	unset url
	unset namespaceInt
	unset prefix
	echo "Egress configured"
done	

egressBody="[${egressBody:1}]"
	
# Write egress config json to file
echo "${egressBody//\\/\\\\}" > "$egressFile"
	

# Update adapter files			
	# Update Adapter Dataselection
		# Not done in this example, but could easily become complex and require input from user to specify what to get.  Could have multiple files that get appended together to make JSON as needed as shown above
		# In this example we assume that the default template is what is needed
		
	# Update Adapter Datasource
		# Not done in this example.  Typically probably need to ask about the IP Address.  Could have multiple files that get appended together to make JSON as needed
			# In this example we assume that the default template is what is needed
		
echo "Getting send folder ready"
# Get the send folder ready
rm -rf ./send		
mkdir -p ./send/installation_files
	
# Copy remote install script
cp -a ./templates/script.sh ./send/script.sh

echo "Copying install file"
# Copy appropriate install 
if [ "$osType" == "1" ] || [ "$osType" == "x64" ]
  then	
    cp -a ./templates/installation_files/EdgeDataStore_linux-x64.deb ./send/installation_files/EdgeDataStore.deb
  else
    cp -a ./templates/installation_files/EdgeDataStore_linux-arm.deb ./send/installation_files/EdgeDataStore.deb
fi

echo "Finalizing send folder"
# Move json files to folder to send across
mv ./$egressFile ./send/$egressFile
mv ./$dataSourceModbus ./send/$dataSourceModbus
mv ./$dataSelectionModbus ./send/$dataSelectionModbus	
mv ./$silentFile ./send/$silentFile

echo "Cleaning up local folder"
# Delete local template copies
rm ./$OCSEgress
rm ./$PIEgress
			
echo "Creating a backup"
# Keep copy around for historical reasons
mkdir -p ./backup/$location
cp -r ./send ./backup/$location
	
# Send files	
echo "Sending files over"
ssh $userName@$location "sudo rm -rf /usr/local/install"
ssh $userName@$location "sudo mkdir -m777 -p /usr/local/install"
scp -r send $userName@$location:/usr/local/install

ssh $userName@$location "sudo chmod -R 755 /usr/local/install"
# Run bash script
echo "Running local script"
ssh $userName@$location /usr/local/install/send/script.sh
