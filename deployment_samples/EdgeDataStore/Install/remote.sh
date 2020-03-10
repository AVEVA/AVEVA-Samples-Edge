#initialize file names to strings
	egressFile="PeriodicEgressEndpoints.json"
	silentFile="silent.ini"
	OCSEgress="OCSEgress.json"
	PIEgress="PIEgress.json"
	dataSourceModbus="Modbus1Datasource.json"
	dataSelectionModbus="Modbus1Dataselection.json"
	
#copy files locally, this ensure the templates stay as templates	
	cp -a ./templates/$dataSourceModbus ./$dataSourceModbus
	cp -a ./templates/$dataSelectionModbus ./$dataSelectionModbus
	cp -a ./templates/$OCSEgress ./$OCSEgress
	cp -a ./templates/$PIEgress ./$PIEgress
	cp -a ./templates/$silentFile ./$silentFile

#Prompt the user for an location file.  
#This allows you to have your location and username and OS information stored somewhere and look it up

	read -p "Do you have an location file configured? (enter file name)  " locationFile	

#If provided location file then use it 
	if [ ! -z "$locationFile" ]
		then	
	#A location file assumes that the file is using linux line endings and the entries are as detailed below
			source <(grep = $locationFile | tr -d "\r")
	fi
	
#If missing parameter or no file entered ask for individual things	
	if [ -z "$location" ]
		then	
			read -p "Where are we installing EDS? (IP Address) " location	
	fi
	
	if [ -z "$userID" ]
		then	
			read -p "Remote Computer User ID?  " userID	
	fi
	
	if [ -z "$osType" ]
		then	
			read -p "What OS type are we sending to?  Linux X64 =1 or x64; Linux Arm 32 =anything else?  " osType	
	fi
	
#Prompt the user for egress information
#In this configuration we can have multiple egress configured, each one differently
	egressBody=""
	read -p "How many egresses should we configure?  " egressToConfigure
			
	for (( i=1; i<=$egressToConfigure; i++ ))
	do
	#There maybe shared information across multiple EDSs or egress endpoints, so to ease interaction we define an egress.txt file
	#For each egress we need to substitute out the placeholders for enterable values
	#Some settings may be shared across egresses, so make these easily reused
		read -p "Do you have an egress configuration file? (enter file name)  " egressConfigFile
		
		if [ ! -z "$egressConfigFile" ]
			then	
		#A egress file assumes that the file is using linux line endings and the entries are as detailed below
				source <(grep = $egressConfigFile | tr -d "\r")
		fi

	#If missing parameter or no file entered ask for individual things				
		if [ -z "$egressType" ]
			then	
				read -p "Is this egress to PI? (y or pi) - all other responses go to OCS  " egressType
		fi
		
		if [ -z "$id" ]
			then	
				read -p "What is the user ID/ClientID?  " id	
		fi
		
		if [ -z "$egressPassword" ]
			then	
				read -p "What is the password/Secret?  " egressPassword
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
		
		if [ -z "$databaseInt" ]
			then	
				read -p "Which database? 1 or default=default all else =diagnostics  " databaseInt		
		fi 
		
		if [ "$databaseInt" == "1" ] || [ "$databaseInt" == "default" ]
			then
				database="default"
			else
				database="diagnostics"	
				prefix+="diagnostics."
		fi		
				
		if [ "$egressType" == "y" ]  || [ "$egressType" == "pi" ]
			then
			#we are configuring pi		
				egress=$(<$PIEgress)
				egressID="pi.${database}"
			else
				egress=$(<$OCSEgress)	
				egressID="ocs.${database}"
		fi
		
		egress="${egress/<egressID>/$egressID}"  
		egress="${egress/<id>/$id}"  
		egress="${egress/<password>/$egressPassword}"  
		egress="${egress/<url>/$url}"  
		egress="${egress/<database>/$database}"  
		egress="${egress//<prefix>/$prefix}"  	
		egressBody+=$egress	
		unset egressConfigFile
		unset id
		unset egressPassword
		unset url
		unset databaseInt
		unset prefix
		echo "Egress configured"
	done	
	
	egressBody="[${egressBody:1}]"
	
#write egress config json to file
	echo "${egressBody//\\/\\\\}" > "$egressFile"
	

#update adapter files			
	#Update Adapter Dataselection
		#Not done in this example, but could easily become complex and require input from user to specify what to get.  Could have multiple files that get appended together to make JSON as needed as shown above
		#In this example we assume that the default template is what is needed
		
	#Update Adapter Datasource
		#Not done in this example.  Typically probably need to ask about the IP Address.  Could have multiple files that get appended together to make JSON as needed
			#In this example we assume that the default template is what is needed
		
echo "Getting send folder ready"
# get the send folder ready
	rm -rf ./send		
	mkdir -p ./send/installation_files
	
#copy remote install script
	cp -a ./templates/script.sh ./send/script.sh

echo "Copying install file"
#copy appropriate install 
	if [ "$osType" == "1" ] || [ "$osType" == "x64" ]
		then	
			cp -a ./templates/installation_files/EdgeDataStore_linux-x64.deb ./send/installation_files/EdgeDataStore.deb
		else
			cp -a ./templates/installation_files/EdgeDataStore_linux-arm.deb./send/installation_files/EdgeDataStore.deb
	fi

echo "Finalizing send folder"
# move jsons to folder to send across
	mv ./$egressFile ./send/$egressFile
	mv ./$dataSourceModbus ./send/$dataSourceModbus
	mv ./$dataSelectionModbus ./send/$dataSelectionModbus
	
	mv ./$silentFile ./send/$silentFile

echo "Cleaning up local folder"
#delete local template copies
	rm ./$OCSEgress
	rm ./$PIEgress
			
echo "Creating a backup"
# keep copy around for historical reasons
	mkdir -p ./backup/$location
	
	cp -r ./send ./backup/$location
	
#send files	
echo "Sending files over"
scp -r send $userID@$location:/usr/local/install

# run bash script
echo "Running local script"
ssh $userID@$location /usr/local/install/send/script.sh
