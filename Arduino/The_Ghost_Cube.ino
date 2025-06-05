#include <WiFiS3.h>
#include <WiFiUdp.h>

const char* ssid = "khoone";
const char* password = "imansamira2019";

const int port = 4211;
WiFiUDP udp;

// Broadcast IP for your network — make sure it matches your router's subnet
IPAddress broadcastIP(192, 168, 0, 255);

// Define buttons
const int buttonPins[3] = {2, 3, 4};
const char* cubeNames[3] = {"A", "B", "C"};
int lastStates[3] = {HIGH, HIGH, HIGH};

void setup() {
  Serial.begin(115200);
  
  for (int i = 0; i < 3; i++) {
    pinMode(buttonPins[i], INPUT_PULLUP);
  }

  WiFi.begin(ssid, password);
  while (WiFi.status() != WL_CONNECTED) {
    delay(1000);
    Serial.println("Connecting to WiFi...");
  }

  Serial.print("Connected. IP: ");
  Serial.println(WiFi.localIP());

  // ✅ This line is required for broadcast to work on Arduino R4 WiFi
  udp.begin(WiFi.localIP(), port);
}

void loop() {
  for (int i = 0; i < 3; i++) {
    int currentState = digitalRead(buttonPins[i]);

    if (currentState != lastStates[i]) {
      if (currentState == LOW) {
        sendMessage((String(cubeNames[i]) + "_appear").c_str());
      } else {
        sendMessage((String(cubeNames[i]) + "_disappear").c_str());
      }
      lastStates[i] = currentState;
      delay(50); // debounce
    }
  }
}

void sendMessage(const char* msg) {
  udp.beginPacket(broadcastIP, port);
  udp.print(msg);
  udp.endPacket();
  Serial.println("Broadcasted: " + String(msg));
  delay(500);
}
