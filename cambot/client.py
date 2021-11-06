import bluetooth
import bluetooth

print("Scanning for bluetooth devices:")
devices = bluetooth.discover_devices(lookup_names = True, lookup_class = True)
number_of_devices = len(devices)
print(number_of_devices,"devices found")
for addr, name, device_class in devices:
    print("\n")
    print("Device:")
    print("Device Name: %s" % (name))
    print("Device MAC Address: %s" % (addr))
    print("Device Class: %s" % (device_class))
    print("\n")
"""
bd_addr = "01:23:45:67:89:AB"

port = 1

sock=bluetooth.BluetoothSocket( bluetooth.RFCOMM )
sock.connect((bd_addr, port))

sock.send("hello!!")

sock.close()
"""