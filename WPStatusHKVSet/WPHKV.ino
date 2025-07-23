// benötigte Libraries: WiFiManager by tzapu, ArdinoJSON by Benoit
// Datei config.json in /data-Verzeichnis; Upload mit LittleFS-Plugin (https://github.com/earlephilhower/arduino-littlefs-upload)
// Beispiel:
// {"WPStatusUrl":"https://waermepumpe.halbinsulaner.de/wpstatus-test.json?e=",  "delaySeconds": 30,  "einheit": "WE99-EG"}
// Nach Erstinstallation einfach mit dem WLAN "ESP-..." verbinden und die Zugangsdaten zum normalen WLAN eingeben.

#include <WiFiManager.h> 
#include <ESP8266HTTPClient.h>
#include "LittleFS.h"
#include <ArduinoJson.h>
#define RELAY 0

WiFiManager wifiManager;
HTTPClient sender;
WiFiClientSecure wifiClient;

String einheit;
int delaySeconds;
String WPStatusUrl;

const int ledPin = 2; // GPIO2 ist mit der internen LED verbunden
void setup() {

  Serial.begin(115200);
  delay(500);
  Serial.println("WärmepumpenStatus-HKV einstellen: Version 1.0");
  // PINS initialisieren
  pinMode(LED_BUILTIN, OUTPUT);  // Initialize the LED_BUILTIN pin as an output
  pinMode(RELAY, OUTPUT);  // Initialize the LED_BUILTIN pin as an output
  digitalWrite(LED_BUILTIN, HIGH); // LED aus (active LOW)
  digitalWrite(RELAY,HIGH); // Relay aus
  // Wifi-Manager initialisieren
  wifiManager.autoConnect();

  // Konfiguration laden aus config.json, gespeichert mit LittleFS

  // Allocate a temporary JsonDocument - Achtung: Codierung muss ANSI sein, nicht UTF-8
  JsonDocument doc;
  // LittleFS-Dateisystem initialisieren
  if( LittleFS.begin() ){
    Serial.println("Dateisystem: initialisiert");
  }else{
    Serial.println("Dateisystem: Fehler beim initialisieren");
  }
  // Datei öffnen
  File file = LittleFS.open("config.json", "r");
  String fileContent = "";
  while ( file.available() ) {
    fileContent += (char)file.read();
  }

  // Deserialize the JSON document
  Serial.print(fileContent);
  DeserializationError error = deserializeJson(doc, fileContent);
  switch (error.code()) {
    case DeserializationError::Ok:
        Serial.print(F("Deserialization succeeded"));
        break;
    case DeserializationError::InvalidInput:
        Serial.print(F("Invalid input!"));
        break;
    case DeserializationError::NoMemory:
        Serial.print(F("Not enough memory"));
        break;
    default:
        Serial.print(F("Deserialization failed"));
        break;
  }
  Serial.println("Konfiguration geladen.");
  
  delaySeconds=doc["delaySeconds"];
  einheit=doc["einheit"].as<String>();
  WPStatusUrl=doc["WPStatusUrl"].as<String>();
  Serial.println("einheit: "+einheit);
  Serial.println("WPStatusUrl: "+WPStatusUrl);
  file.close();
  // WifiClient auf insecure setzen, da wir keine CA-Zertifikate gespeichert haben
  wifiClient.setInsecure();
}

void loop() {

  // Status Wärmepumpe holen, zurückgegeben wird ein JSON-Dokument
  // [{"status":"KÜHLEN","statusdate":"20.07.2025 17:22"},{"status":"KÜH

  Serial.println("Hole "+WPStatusUrl+" ..."); 
  if (sender.begin(wifiClient, WPStatusUrl)) {
    
    Serial.println("Website holen..."); 
    // HTTP-Code der Response speichern
    int httpCode = sender.GET();
    Serial.println("http-Code:"); 
    Serial.println(httpCode); 

    if (httpCode > 0) {
      Serial.println("Antwort da..."); 
      Serial.println(httpCode); 
      // Anfrage wurde gesendet und Server hat geantwortet
      // Info: Der HTTP-Code für 'OK' ist 200
      if (httpCode == HTTP_CODE_OK) {
       
        // String vom Webseiteninhalt speichern
        String payload = sender.getString();

        Serial.println(payload);
        String lastStat = payload.substring(0,30);
        int value = HIGH; 
        if(lastStat.indexOf("HEIZEN") > 0) 
        {
            //Status Heizen = Relais AUS
            Serial.println("HEIZEN: RELAY=OFF"); 
            digitalWrite(LED_BUILTIN, HIGH); // LED wird hier ausgeschaltet (active LOW)
            Serial.println("LED Aus"); 
            digitalWrite(RELAY,HIGH); 
            Serial.println("Relay High"); 
            value = HIGH;
        }
        else
        {
          // Status Kühlen = Relais AN
          Serial.println("KÜHLEN: RELAY=ON"); 
          digitalWrite(LED_BUILTIN, LOW); // LED wird hier eingeschaltet (active LOW)
          Serial.println("LED Ein"); 
          digitalWrite(RELAY,LOW); 
          Serial.println("Relay Low"); 
          value = LOW; 
        }
        
        
        
      }
      else
      {
      // Falls HTTP-Error
      Serial.printf("HTTP-Error: ", sender.errorToString(httpCode).c_str());
      }
    }
    else
    {
    // Falls HTTP-Error
    Serial.printf("HTTP-Error: Rückgabecode <0");
    }
    
    // Wenn alles abgeschlossen ist, wird die Verbindung wieder beendet
    Serial.println("Request zu Ende"); 
    sender.end();
    //Schlafen legen gem. JSON-Datei. 4 Stunden = 4*60*60 = 14400 Sekunden
    
    Serial.println("Jetzt schlafen: ");
    Serial.println(delaySeconds);
    Serial.println("Sekunden"); 
    //delaySeconds=30;
    delay(delaySeconds * 1000UL); 
  }
}


