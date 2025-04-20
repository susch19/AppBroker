CANAME=Smarthome
# optional, create a directory
mkdir $CANAME
cd $CANAME
# generate aes encrypted private key
openssl genrsa -aes256 -out $CANAME.key 4096
# create certificate, 1826 days = 5 years
openssl req -x509 -new -nodes -key $CANAME.key -sha256 -days 36500 -out $CANAME.crt -subj '/CN=Smarthome Root CA/C=DE/ST=/L=/O=susch'
