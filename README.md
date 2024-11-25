erDiagram
  USER {
    int UserID
    string Name
    string Contact
    int VehicleID
  }
  VEHICLE {
    int VehicleID
    string Description
    string PlateNumber
  }
  SERVICE_PROVIDER {
    int ProviderID
    string Name
    string Contact
    int VehicleID
  }
  REQUESTS {
    int RequestID
    int UserID
    int ProviderID
    float Price
    string Status
    datetime Datetime
  }
  USER ||--|{ VEHICLE : "owns"
  REQUESTS }|--|| SERVICE_PROVIDER : "handled by"
  REQUESTS }|--|| USER : "made by"
