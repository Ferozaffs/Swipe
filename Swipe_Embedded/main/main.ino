
#include <CodeCell.h>
#include <ArduinoBLE.h>

CodeCell cell;

BLEService bleService("156F");
BLEStringCharacteristic bleCharacteristic("2C19", BLERead | BLENotify, 96);

void setup() {
  Serial.begin(115200);

  cell.Init(MOTION_LINEAR_ACC + LIGHT); 

  Serial.println("Cell: started");

  if (!BLE.begin()) {
    Serial.println("BT: failed to initialize BLE!");
    while (1) {
      cell.LED(0x05, 0, 0);
    }
  }

  BLE.setLocalName("Swipe v.1.0");
  BLE.setAdvertisedService(bleService);
  bleService.addCharacteristic(bleCharacteristic);
  BLE.addService(bleService);
  bleCharacteristic.writeValue("0.0,0.0,0.0,0.0");

  BLE.advertise();

  Serial.println("BT: advertising");
}

void loop() {
  unsigned long epochTime = millis() / 1000;

  BLEDevice central = BLE.central();
  if (central) {
    Serial.print("BT: connected to central: ");
    Serial.println(central.address());

    while (central.connected()) {
      epochTime = millis() / 1000;
      if(epochTime % 10 == 0) {
        cell.LED(0, 0x05, 0);
      } 
      else {
        cell.LED(0, 0, 0);
      }

      if (cell.Run(60)) {
        float x = 0.0;
        float y = 0.0;
        float z = 0.0;
        cell.Motion_LinearAccRead(x, y, z);
        uint16_t proximity = cell.Light_ProximityRead();
        
        String data = ">LinAccel_x:" + String(x) + "," + ">LinAccel_y:" + String(y) + "," + ">LinAccel_z:" + String(z) + "," + ">Proximity:" + proximity;
        bleCharacteristic.writeValue(data); 
      }
    }

    Serial.print("BT: disconnected from central: ");
    Serial.println(central.address()); 
  }

  if(epochTime % 10 == 0) {
    cell.LED(0, 0x05, 0x05);
  } 
  else {
    cell.LED(0, 0, 0);
  }
}
