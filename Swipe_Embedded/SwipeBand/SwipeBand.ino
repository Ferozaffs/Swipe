#define DEBUG

#ifdef DEBUG
  #define DEBUG_PRINT(x) Serial.print(x)
  #define DEBUG_PRINTLN(x) Serial.println(x)
#else
  #define DEBUG_PRINT(x)
  #define DEBUG_PRINTLN(x)
#endif

#include <CodeCell.h>
#include <ArduinoBLE.h>

CodeCell cell;

BLEService bleService("13de80c4-cdd2-496e-901d-6537ea44f3d8");
BLEStringCharacteristic bleCharacteristic("b6fe5a0e-ff8d-494a-8885-143cda0100ea", BLERead | BLENotify, 96);

void setup() {
#ifdef DEBUG
    Serial.begin(115200);
#endif


  cell.Init(MOTION_LINEAR_ACC + LIGHT); 

  DEBUG_PRINTLN("Cell: started");

  if (!BLE.begin()) {
    DEBUG_PRINTLN("BT: failed to initialize BLE!");
    while (1) {
      cell.LED(1U, 0, 0);
    }
  }

  BLE.setLocalName("Swipe Band v.1.0");
  BLE.setAdvertisedService(bleService);
  bleService.addCharacteristic(bleCharacteristic);
  BLE.addService(bleService);
  bleCharacteristic.writeValue("0.0,0.0,0.0,0.0");

  BLE.advertise();

  DEBUG_PRINTLN("BT: advertising");
}

void loop() {
  if(cell.PowerStateRead() == 0)
  {
    unsigned long epochTime = millis() / 1000;

    BLEDevice central = BLE.central();
    if (central) {
      DEBUG_PRINT("BT: connected to central: ");
      DEBUG_PRINTLN(central.address());

      while (central.connected()) {
        epochTime = millis() / 1000;
        if(epochTime % 10 == 0) {
          cell.LED(0, 1U, 0);
        } 
        else if(epochTime % 10 == 2){
          cell.LED(0, 0, 0);
        }

        if (cell.Run(60)) {
          float x = 0.0;
          float y = 0.0;
          float z = 0.0;
          cell.Motion_LinearAccRead(x, y, z);
          float proximity = cell.Light_ProximityRead() * 0.002;
          
          String data = ">LinAccel_x:" + String(x) + "," + ">LinAccel_y:" + String(y) + "," + ">LinAccel_z:" + String(z) + "," + ">Proximity:" + proximity;
          bleCharacteristic.writeValue(data); 
        }
      }

      DEBUG_PRINT("BT: disconnected from central: ");
      DEBUG_PRINTLN(central.address()); 
    }

    cell.Run(1);
    if(epochTime % 10 == 0) {
      cell.LED(0, 1U, 1U);
    } 
    else if(epochTime % 10 == 2) { 
      cell.LED(0, 0, 0);
    }
  }
  else 
  {
    cell.Run(1);
  }
}
