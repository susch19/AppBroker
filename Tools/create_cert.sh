CANAME=Smarthome
cd $CANAME
# create certificate for service
MYCERT=wildcard
openssl req -new -nodes -out $MYCERT.csr -newkey rsa:4096 -keyout $MYCERT.key -subj "/CN=${MYCERT}/C=DE/ST=/L=/O=susch"
# create a v3 ext file for SAN properties
cat > $MYCERT.v3.ext << EOF
authorityKeyIdentifier=keyid,issuer
basicConstraints=CA:FALSE
keyUsage = digitalSignature, nonRepudiation, keyEncipherment, dataEncipherment
subjectAltName = @alt_names
[alt_names]
DNS.1 = *
IP.1 = 192.168.49.22
EOF
openssl x509 -req -in $MYCERT.csr -CA $CANAME.crt -CAkey $CANAME.key -CAcreateserial -out $MYCERT.crt -days 730 -sha256 -extfile $MYCERT.v3.ext
openssl pkcs12 -export -out $MYCERT.pfx -inkey $MYCERT.key -in $MYCERT.crt