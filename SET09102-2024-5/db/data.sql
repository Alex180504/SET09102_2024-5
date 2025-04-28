USE sensor_monitoring;

-- Ensure role_name is unique to prevent subquery errors
ALTER TABLE role ADD UNIQUE (role_name);

-- Ensure we have default roles
INSERT INTO role (role_name, description) VALUES 
('Administrator', 'Full system access with all privileges'),
('Environmental Scientist', 'Access to data analysis, reports, and alerts'),
('Operations Manager', 'Monitor sensors and schedule maintenance'),
('Field Technician', 'View sensor details and perform maintenance'),
('Guest', 'Limited read-only access')
ON DUPLICATE KEY UPDATE 
    description = VALUES(description);

-- Define access privileges for different system modules
INSERT INTO access_privilege (name, description, module_name) VALUES
-- User Management
('user.view', 'View user accounts', 'User Management'),
('user.create', 'Create new user accounts', 'User Management'),
('user.edit', 'Edit user account details', 'User Management'),
('user.delete', 'Delete user accounts', 'User Management'),

-- Role Management
('role.view', 'View roles and privileges', 'Role Management'),
('role.create', 'Create new roles', 'Role Management'),
('role.edit', 'Edit role details and privileges', 'Role Management'),
('role.delete', 'Delete roles', 'Role Management'),
('role.assign', 'Assign roles to users', 'Role Management'),

-- Sensor Management
('sensor.view', 'View sensor details', 'Sensor Management'),
('sensor.create', 'Add new sensors to the system', 'Sensor Management'),
('sensor.edit', 'Edit sensor configuration', 'Sensor Management'),
('sensor.delete', 'Remove sensors from the system', 'Sensor Management'),

-- Data Access
('data.view', 'View environmental data', 'Data Access'),
('data.analyze', 'Perform data analysis', 'Data Access'),
('data.export', 'Export data from the system', 'Data Access'),

-- Alerts
('alert.view', 'View system alerts', 'Alerts'),
('alert.respond', 'Respond to alerts', 'Alerts'),
('alert.configure', 'Configure alert thresholds', 'Alerts'),

-- Maintenance
('maintenance.view', 'View maintenance records', 'Maintenance'),
('maintenance.schedule', 'Schedule maintenance tasks', 'Maintenance'),
('maintenance.perform', 'Record completed maintenance', 'Maintenance'),

-- Reports
('report.view', 'View reports', 'Reports'),
('report.create', 'Create new reports', 'Reports'),
('report.export', 'Export reports', 'Reports'),

-- System Configuration
('system.configure', 'Configure system settings', 'System'),
('system.backup', 'Create and restore backups', 'System'),
('system.logs', 'Access system logs', 'System')
ON DUPLICATE KEY UPDATE 
    description = VALUES(description),
    module_name = VALUES(module_name);

-- Assign all privileges to Administrator role
INSERT INTO role_privilege (role_id, access_privilege_id)
SELECT 
    (SELECT role_id FROM role WHERE role_name = 'Administrator' LIMIT 1), 
    access_privilege_id 
FROM access_privilege
ON DUPLICATE KEY UPDATE role_id = role_id;

-- Assign privileges for Environmental Scientist
INSERT INTO role_privilege (role_id, access_privilege_id)
SELECT 
    (SELECT role_id FROM role WHERE role_name = 'Environmental Scientist' LIMIT 1), 
    access_privilege_id 
FROM access_privilege 
WHERE name IN (
    'sensor.view', 'data.view', 'data.analyze', 'data.export', 
    'alert.view', 'alert.respond', 'report.view', 'report.create', 'report.export'
)
ON DUPLICATE KEY UPDATE role_id = role_id;

-- Assign privileges for Operations Manager
INSERT INTO role_privilege (role_id, access_privilege_id)
SELECT 
    (SELECT role_id FROM role WHERE role_name = 'Operations Manager' LIMIT 1), 
    access_privilege_id 
FROM access_privilege 
WHERE name IN (
    'sensor.view', 'sensor.edit', 'data.view', 
    'alert.view', 'alert.respond', 'alert.configure',
    'maintenance.view', 'maintenance.schedule', 'report.view'
)
ON DUPLICATE KEY UPDATE role_id = role_id;

-- Assign privileges for Field Technician
INSERT INTO role_privilege (role_id, access_privilege_id)
SELECT 
    (SELECT role_id FROM role WHERE role_name = 'Field Technician' LIMIT 1), 
    access_privilege_id 
FROM access_privilege 
WHERE name IN (
    'sensor.view', 'maintenance.view', 'maintenance.perform'
)
ON DUPLICATE KEY UPDATE role_id = role_id;

-- Assign limited privileges to Guest role
INSERT INTO role_privilege (role_id, access_privilege_id)
SELECT 
    (SELECT role_id FROM role WHERE role_name = 'Guest' LIMIT 1), 
    access_privilege_id 
FROM access_privilege 
WHERE name IN (
    'sensor.view', 'data.view'
)
ON DUPLICATE KEY UPDATE role_id = role_id;

-- Ensure at least one administrator user exists
INSERT INTO user (first_name, last_name, email, role_id, password_hash, password_salt)
SELECT 'Admin', 'User', 'admin@example.com', 
       (SELECT role_id FROM role WHERE role_name = 'Administrator' LIMIT 1),
       'HSN0y+hZ6jK50zwJPrBe0p+1qlO2ppxCP5Jlo63jmyXkhk4w+i+P/TTLucywy7qg3BRYQBus7WN+4sZyocsXcw==',
       '4bz835D7mDzxTvpNbfPSSJXzKHq6/lizjm0nY8HjEX3YfdDXoHLXneAI6KY5Jk3HQ1VaY2bDEvMo5cv2GthL7iua6ExwQYGWM9HQDcf2CKHIvsnxvIHA06XzrpPUH/429sNOgb/awkn95WeAiTjRThStw2YdZxTc7ssVi8uHbTk='
FROM dual 
WHERE NOT EXISTS(SELECT 1 FROM user WHERE email = 'admin@example.com');

-- Ensure we have a test user for each role
INSERT INTO user (first_name, last_name, email, role_id, password_hash, password_salt)
SELECT 'Scientist', 'User', 'scientist@example.com', 
       (SELECT role_id FROM role WHERE role_name = 'Environmental Scientist' LIMIT 1),
       'HSN0y+hZ6jK50zwJPrBe0p+1qlO2ppxCP5Jlo63jmyXkhk4w+i+P/TTLucywy7qg3BRYQBus7WN+4sZyocsXcw==',
       '4bz835D7mDzxTvpNbfPSSJXzKHq6/lizjm0nY8HjEX3YfdDXoHLXneAI6KY5Jk3HQ1VaY2bDEvMo5cv2GthL7iua6ExwQYGWM9HQDcf2CKHIvsnxvIHA06XzrpPUH/429sNOgb/awkn95WeAiTjRThStw2YdZxTc7ssVi8uHbTk='
FROM dual 
WHERE NOT EXISTS(SELECT 1 FROM user WHERE email = 'scientist@example.com');

INSERT INTO user (first_name, last_name, email, role_id, password_hash, password_salt)
SELECT 'Operations', 'Manager', 'operations@example.com', 
       (SELECT role_id FROM role WHERE role_name = 'Operations Manager' LIMIT 1),
       'HSN0y+hZ6jK50zwJPrBe0p+1qlO2ppxCP5Jlo63jmyXkhk4w+i+P/TTLucywy7qg3BRYQBus7WN+4sZyocsXcw==',
       '4bz835D7mDzxTvpNbfPSSJXzKHq6/lizjm0nY8HjEX3YfdDXoHLXneAI6KY5Jk3HQ1VaY2bDEvMo5cv2GthL7iua6ExwQYGWM9HQDcf2CKHIvsnxvIHA06XzrpPUH/429sNOgb/awkn95WeAiTjRThStw2YdZxTc7ssVi8uHbTk='
FROM dual 
WHERE NOT EXISTS(SELECT 1 FROM user WHERE email = 'operations@example.com');

INSERT INTO user (first_name, last_name, email, role_id, password_hash, password_salt)
SELECT 'Tech', 'Support', 'technician@example.com', 
       (SELECT role_id FROM role WHERE role_name = 'Field Technician' LIMIT 1),
       'HSN0y+hZ6jK50zwJPrBe0p+1qlO2ppxCP5Jlo63jmyXkhk4w+i+P/TTLucywy7qg3BRYQBus7WN+4sZyocsXcw==',
       '4bz835D7mDzxTvpNbfPSSJXzKHq6/lizjm0nY8HjEX3YfdDXoHLXneAI6KY5Jk3HQ1VaY2bDEvMo5cv2GthL7iua6ExwQYGWM9HQDcf2CKHIvsnxvIHA06XzrpPUH/429sNOgb/awkn95WeAiTjRThStw2YdZxTc7ssVi8uHbTk='
FROM dual 
WHERE NOT EXISTS(SELECT 1 FROM user WHERE email = 'technician@example.com');

-- Measurands with appropriate quantity types, names, symbols and units
INSERT INTO measurand (measurand_id, quantity_type, quantity_name, symbol, unit) VALUES
-- Air Quality Measurands
(1, 'Concentration', 'Particulate Matter 2.5', 'PM2.5', 'µg/m³'),
(2, 'Concentration', 'Carbon Dioxide', 'CO2', 'ppm'),
(3, 'Concentration', 'Ozone', 'O3', 'ppb'),
(4, 'Concentration', 'Nitrogen Dioxide', 'NO2', 'ppb'),
(5, 'Concentration', 'Sulfur Dioxide', 'SO2', 'ppb'),
-- Water Quality Measurands
(6, 'Chemical', 'pH Level', 'pH', '-'),
(7, 'Physical', 'Turbidity', 'Turb', 'NTU'),
(8, 'Chemical', 'Dissolved Oxygen', 'DO', 'mg/L'),
(9, 'Physical', 'Conductivity', 'Cond', 'µS/cm'),
(10, 'Physical', 'Water Temperature', 'Temp', '°C'),
-- Weather Measurands
(11, 'Meteorological', 'Air Temperature', 'Temp', '°C'),
(12, 'Meteorological', 'Relative Humidity', 'RH', '%'),
(13, 'Meteorological', 'Wind Speed', 'Wind', 'km/h'),
(14, 'Meteorological', 'Precipitation Rate', 'Precip', 'mm/h'),
(15, 'Meteorological', 'Barometric Pressure', 'Press', 'hPa');

-- Air Quality Sensors - using actual sensor models
INSERT INTO sensor (sensor_id, sensor_type, status, deployment_date, measurand_id) VALUES
(1, 'Enviro+ Particulate', 'Active', '2023-10-15', 1),
(2, 'SenseAir S8', 'Active', '2023-10-20', 2),
(3, 'Aeroqual SM50', 'Active', '2023-11-05', 3),
(4, 'Alphasense NO2-B43F', 'Maintenance', '2023-09-10', 4),
(5, 'Alphasense SO2-B4', 'Active', '2023-12-01', 5);

-- Water Quality Sensors - using actual sensor models
INSERT INTO sensor (sensor_id, sensor_type, status, deployment_date, measurand_id) VALUES
(6, 'Atlas Scientific pH', 'Active', '2023-08-25', 6),
(7, 'DFRobot SEN0189', 'Active', '2023-09-30', 7),
(8, 'Atlas Scientific DO', 'Error', '2023-07-15', 8),
(9, 'DFRobot DFR0300', 'Active', '2023-11-20', 9),
(10, 'DS18B20 Waterproof', 'Inactive', '2023-06-05', 10);

-- Weather Condition Sensors - using actual sensor models
INSERT INTO sensor (sensor_id, sensor_type, status, deployment_date, measurand_id) VALUES
(11, 'Davis Instruments 6830', 'Active', '2023-10-01', 11),
(12, 'Sensirion SHT31-D', 'Active', '2023-10-01', 12),
(13, 'Young 05103 Anemometer', 'Active', '2023-10-05', 13),
(14, 'Hydreon RG-11', 'Active', '2023-10-10', 14),
(15, 'Bosch BMP388', 'Maintenance', '2023-09-15', 15);

-- Configuration for sensors
INSERT INTO configuration (sensor_id, latitude, longitude, altitude, orientation, measurement_frequency, min_threshold, max_threshold) VALUES
(1, 55.9533, -3.1883, 80.5, 0, 15, 0, 100),    
(2, 55.8642, -4.2518, 40.2, 45, 10, 350, 2000), 
(3, 57.1497, -2.0943, 65.0, 90, 30, 0, 150),  
(4, 56.4907, -2.9977, 22.3, 180, 15, 0, 200),
(5, 55.7772, -4.0558, 50.1, 270, 30, 0, 200),
(6, 56.1165, -3.9369, 15.2, 225, 60, 0, 14),    
(7, 55.9419, -3.2096, 10.5, 135, 60, 0, 20), 
(8, 57.4796, -4.2249, 5.3, 315, 30, 0, 15),   
(9, 55.8415, -4.4638, 8.7, 90, 60, 0, 2000),
(10, 56.0011, -3.7849, 12.4, 0, 60, -5, 30),  
(11, 55.8279, -4.4314, 25.6, 180, 15, -30, 50),
(12, 55.9928, -3.1712, 40.8, 90, 15, 0, 100),
(13, 57.6500, -3.3167, 10.2, 45, 10, 0, 150),
(14, 56.1881, -3.1789, 35.9, 270, 15, 0, 50),
(15, 55.4643, -2.8744, 120.3, 315, 15, 980, 1050); 

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

-- Sample measurements for each sensor
INSERT INTO measurement (measurement_id, timestamp, value, sensor_id) VALUES
-- Air Quality measurements
(1, '2023-12-10 08:00:00', 18.5, 1),  -- PM2.5
(2, '2023-12-10 08:15:00', 17.9, 1),
(3, '2023-12-10 08:30:00', 19.2, 1),
(4, '2023-12-10 08:00:00', 620.3, 2), -- CO2
(5, '2023-12-10 08:10:00', 615.8, 2),
(6, '2023-12-10 08:20:00', 635.2, 2),
(7, '2023-12-10 08:00:00', 45.2, 3),  -- Ozone
(8, '2023-12-10 08:30:00', 46.8, 3),
(9, '2023-12-10 09:00:00', 44.5, 3),
-- Water Quality measurements
(10, '2023-12-10 08:00:00', 7.2, 6),  -- pH
(11, '2023-12-10 09:00:00', 7.3, 6),
(12, '2023-12-10 10:00:00', 7.1, 6),
(13, '2023-12-10 08:00:00', 8.2, 8),  -- DO
(14, '2023-12-10 08:30:00', 8.0, 8),
(15, '2023-12-10 09:00:00', 7.8, 8),
-- Weather measurements
(16, '2023-12-10 08:00:00', 12.5, 11), -- Air Temperature
(17, '2023-12-10 08:15:00', 13.1, 11),
(18, '2023-12-10 08:30:00', 13.8, 11),
(19, '2023-12-10 08:00:00', 65.2, 12), -- Humidity
(20, '2023-12-10 08:15:00', 64.8, 12),
(21, '2023-12-10 08:30:00', 66.5, 12);

-- Sample incidents based on measurements
INSERT INTO incident (incident_id, responder_id, responder_comments, resolved_date, priority) VALUES
(1, 1, 'High PM2.5 levels detected. Investigated local construction activity as possible cause.', '2023-12-10', 'Medium'),
(2, 3, 'Low dissolved oxygen in water. Added aerators to affected area.', '2023-12-11', 'High'),
(3, 2, 'Higher than normal CO2 levels. Building ventilation issue fixed.', NULL, 'Low');

-- Link incidents to relevant measurements
INSERT INTO incident_measurement (measurement_id, incident_id) VALUES
(1, 1), -- PM2.5 measurement linked to first incident
(2, 1), -- Another PM2.5 measurement linked to first incident
(14, 2), -- DO measurement linked to second incident
(15, 2), -- Another DO measurement linked to second incident
(4, 3), -- CO2 measurement linked to third incident
(5, 3); -- Another CO2 measurement linked to third incident

-- Sample maintenance records
INSERT INTO maintenance (maintenance_id, maintenance_date, maintainer_id, sensor_id, maintainer_comments) VALUES
(1, '2023-11-15', 2, 4, 'Replaced sensor filter and recalibrated.'),
(2, '2023-11-20', 1, 8, 'Cleaned sensor probe and updated firmware.'),
(3, '2023-12-05', 3, 15, 'Adjusted mounting bracket and verified readings.');