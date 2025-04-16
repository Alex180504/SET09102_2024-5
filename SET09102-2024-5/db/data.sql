-- Add sample roles
INSERT INTO role (role_name) VALUES
('operations_manager'),
('administrator'),
('env_scientist');

-- Add sample users
INSERT INTO user (first_name, last_name, email, role_id) VALUES
('John', 'Manager', 'john.manager@example.com', 1),
('Sarah', 'Admin', 'sarah.admin@example.com', 2),
('Michael', 'Scientist', 'michael.scientist@example.com', 3);

-- Air Quality Sensors
INSERT INTO sensor (sensor_type, status, deployment_date) VALUES
('Air Quality - PM2.5', 'Active', '2023-10-15'),
('Air Quality - CO2', 'Active', '2023-10-20'),
('Air Quality - Ozone', 'Active', '2023-11-05'),
('Air Quality - NO2', 'Maintenance', '2023-09-10'),
('Air Quality - SO2', 'Active', '2023-12-01');

-- Water Quality Sensors
INSERT INTO sensor (sensor_type, status, deployment_date) VALUES
('Water Quality - pH', 'Active', '2023-08-25'),
('Water Quality - Turbidity', 'Active', '2023-09-30'),
('Water Quality - Dissolved Oxygen', 'Error', '2023-07-15'),
('Water Quality - Conductivity', 'Active', '2023-11-20'),
('Water Quality - Temperature', 'Inactive', '2023-06-05');

-- Weather Condition Sensors
INSERT INTO sensor (sensor_type, status, deployment_date) VALUES
('Weather - Temperature', 'Active', '2023-10-01'),
('Weather - Humidity', 'Active', '2023-10-01'),
('Weather - Wind Speed', 'Active', '2023-10-05'),
('Weather - Precipitation', 'Active', '2023-10-10'),
('Weather - Barometric Pressure', 'Maintenance', '2023-09-15');

-- Configuration for sensors (latitude, longitude, altitude, etc.)
INSERT INTO configuration (sensor_id, latitude, longitude, altitude, orientation, measurment_frequency, min_threshold, max_threshold, reading_format) VALUES
(1, 55.9533, -3.1883, 80.5, 'North', 15, 0, 100, 'µg/m³'),     -- PM2.5 in Edinburgh
(2, 55.8642, -4.2518, 40.2, 'Northeast', 10, 350, 2000, 'ppm'), -- CO2 in Glasgow
(3, 57.1497, -2.0943, 65.0, 'East', 30, 0, 150, 'ppb'),         -- Ozone in Aberdeen
(4, 56.4907, -2.9977, 22.3, 'South', 15, 0, 200, 'ppb'),        -- NO2 in Dundee
(5, 55.7772, -4.0558, 50.1, 'West', 30, 0, 200, 'ppb'),         -- SO2 in Motherwell
(6, 56.1165, -3.9369, 15.2, 'Southwest', 60, 0, 14, 'pH'),      -- pH in Falkirk
(7, 55.9419, -3.2096, 10.5, 'Southeast', 60, 0, 20, 'NTU'),     -- Turbidity in Edinburgh River
(8, 57.4796, -4.2249, 5.3, 'Northwest', 30, 0, 15, 'mg/L'),     -- DO in Inverness
(9, 55.8415, -4.4638, 8.7, 'East', 60, 0, 2000, 'µS/cm'),       -- Conductivity in Paisley
(10, 56.0011, -3.7849, 12.4, 'North', 60, -5, 30, '°C'),        -- Water Temp in Linlithgow
(11, 55.8279, -4.4314, 25.6, 'South', 15, -30, 50, '°C'),       -- Air Temp in Glasgow
(12, 55.9928, -3.1712, 40.8, 'East', 15, 0, 100, '%'),          -- Humidity in Edinburgh
(13, 57.6500, -3.3167, 10.2, 'Northeast', 10, 0, 150, 'km/h'),  -- Wind Speed in Elgin
(14, 56.1881, -3.1789, 35.9, 'West', 15, 0, 50, 'mm/h'),        -- Precipitation in Dunfermline
(15, 55.4643, -2.8744, 120.3, 'Northwest', 15, 980, 1050, 'hPa'); -- Pressure in Galashiels

-- Firmware information for each sensor
INSERT INTO sensor_firmware (sensor_id, firmware_version, last_update_date) VALUES
(1, 'v2.1.5', '2023-09-20'),
(2, 'v3.0.2', '2023-10-05'),
(3, 'v2.0.8', '2023-08-15'),
(4, 'v1.9.7', '2023-07-30'),
(5, 'v2.5.1', '2023-11-10'),
(6, 'v4.0.3', '2023-08-01'),
(7, 'v3.2.1', '2023-09-15'),
(8, 'v2.8.5', '2023-06-20'),
(9, 'v3.0.0', '2023-10-25'),
(10, 'v2.2.4', '2023-05-30'),
(11, 'v4.1.2', '2023-09-25'),
(12, 'v4.1.2', '2023-09-25'),
(13, 'v3.5.7', '2023-09-10'),
(14, 'v3.2.3', '2023-10-01'),
(15, 'v2.9.9', '2023-08-20');

-- Measurand types for each sensor (what each sensor measures)
INSERT INTO measurand (sensor_id, quantity_type, quantity_name) VALUES
-- Air Quality Sensors
(1, 'Concentration', 'Particulate Matter 2.5'),
(2, 'Concentration', 'Carbon Dioxide'),
(3, 'Concentration', 'Ozone'),
(4, 'Concentration', 'Nitrogen Dioxide'),
(5, 'Concentration', 'Sulfur Dioxide'),
-- Water Quality Sensors
(6, 'Chemical', 'pH Level'),
(7, 'Physical', 'Turbidity'),
(8, 'Chemical', 'Dissolved Oxygen'),
(9, 'Physical', 'Conductivity'),
(10, 'Physical', 'Water Temperature'),
-- Weather Sensors
(11, 'Meteorological', 'Air Temperature'),
(12, 'Meteorological', 'Relative Humidity'),
(13, 'Meteorological', 'Wind Speed'),
(14, 'Meteorological', 'Precipitation Rate'),
(15, 'Meteorological', 'Barometric Pressure');

-- Sample measurements for each measurand
INSERT INTO measurement (timestamp, value, quantity_id) VALUES
-- Air Quality measurements
('2023-12-10 08:00:00', 18.5, 1),  -- PM2.5
('2023-12-10 08:15:00', 17.9, 1),
('2023-12-10 08:30:00', 19.2, 1),
('2023-12-10 08:00:00', 620.3, 2), -- CO2
('2023-12-10 08:10:00', 615.8, 2),
('2023-12-10 08:20:00', 635.2, 2),
('2023-12-10 08:00:00', 45.2, 3),  -- Ozone
('2023-12-10 08:30:00', 46.8, 3),
('2023-12-10 09:00:00', 44.5, 3),
-- Water Quality measurements
('2023-12-10 08:00:00', 7.2, 6),   -- pH
('2023-12-10 09:00:00', 7.3, 6),
('2023-12-10 10:00:00', 7.1, 6),
('2023-12-10 08:00:00', 8.2, 8),   -- DO
('2023-12-10 08:30:00', 8.0, 8),
('2023-12-10 09:00:00', 7.8, 8),
-- Weather measurements
('2023-12-10 08:00:00', 12.5, 11), -- Air Temperature
('2023-12-10 08:15:00', 13.1, 11),
('2023-12-10 08:30:00', 13.8, 11),
('2023-12-10 08:00:00', 65.2, 12), -- Humidity
('2023-12-10 08:15:00', 64.8, 12),
('2023-12-10 08:30:00', 66.5, 12);

-- Sample incidents based on measurements
INSERT INTO incident (responder_id, responder_comments, resolved_date, priority) VALUES
(1, 'High PM2.5 levels detected. Investigated local construction activity as possible cause.', '2023-12-10', 'Medium'),
(3, 'Low dissolved oxygen in water. Added aerators to affected area.', '2023-12-11', 'High'),
(2, 'Higher than normal CO2 levels. Building ventilation issue fixed.', NULL, 'Low');

-- Link incidents to relevant measurements
INSERT INTO incident_measurement (measurement_id, incident_id) VALUES
(1, 1), -- PM2.5 measurement linked to first incident
(2, 1), -- Another PM2.5 measurement linked to first incident
(14, 2), -- DO measurement linked to second incident
(15, 2), -- Another DO measurement linked to second incident
(4, 3), -- CO2 measurement linked to third incident
(5, 3); -- Another CO2 measurement linked to third incident

-- Sample maintenance records
INSERT INTO maintenance (maintenance_date, maintainer_id, sensor_id, maintainer_comments) VALUES
('2023-11-15', 2, 4, 'Replaced sensor filter and recalibrated.'),
('2023-11-20', 1, 8, 'Cleaned sensor probe and updated firmware.'),
('2023-12-05', 3, 15, 'Adjusted mounting bracket and verified readings.');

