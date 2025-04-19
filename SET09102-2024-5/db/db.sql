CREATE DATABASE IF NOT EXISTS sensor_monitoring;
USE sensor_monitoring;

-- Role Table
CREATE TABLE role (
    role_id INT AUTO_INCREMENT PRIMARY KEY,
    role_name VARCHAR(100) NOT NULL,
);

-- User Table
CREATE TABLE user (
    user_id INT AUTO_INCREMENT PRIMARY KEY,
    first_name VARCHAR(100),
    last_name VARCHAR(100),
    email VARCHAR(255),
    role_id INT NOT NULL,
    password_hash VARCHAR(255),
    password_salt VARCHAR(255),
    FOREIGN KEY (role_id) REFERENCES role(role_id) ON DELETE RESTRICT
);

-- Sensor Table
CREATE TABLE sensor (
    sensor_id INT AUTO_INCREMENT PRIMARY KEY,
    sensor_type VARCHAR(100),
    status VARCHAR(50),
    deployment_date DATE
);

-- Configuration Setting Table
CREATE TABLE configuration_setting (
    setting_id INT AUTO_INCREMENT PRIMARY KEY,
    sensor_id INT NOT NULL,
    setting_name VARCHAR(100),
    minimum_value FLOAT,
    maximum_value FLOAT,
    current_value FLOAT,
    FOREIGN KEY (sensor_id) REFERENCES sensor(sensor_id) ON DELETE CASCADE
);

-- Maintenance Table
CREATE TABLE maintenance (
    maintenance_id INT AUTO_INCREMENT PRIMARY KEY,
    sensor_id INT NOT NULL,
    maintenance_date DATE,
    maintainer_id INT NOT NULL,
    maintainer_comments TEXT,
    FOREIGN KEY (sensor_id) REFERENCES sensor(sensor_id) ON DELETE CASCADE,
    FOREIGN KEY (maintainer_id) REFERENCES user(user_id) ON DELETE CASCADE
);

-- Physical Quantity Table
CREATE TABLE physical_quantity (
    quantity_id INT AUTO_INCREMENT PRIMARY KEY,
    sensor_id INT NOT NULL,
    lower_warning_threshold FLOAT,
    upper_warning_threshold FLOAT,
    upper_emergency_threshold FLOAT,
    lower_emergency_threshold FLOAT,
    quantity_name VARCHAR(100),
    FOREIGN KEY (sensor_id) REFERENCES sensor(sensor_id) ON DELETE CASCADE
);

-- Measurement Table
CREATE TABLE measurement (
    measurement_id INT AUTO_INCREMENT PRIMARY KEY,
    timestamp DATETIME,
    unit_of_measurement VARCHAR(50),
    value FLOAT,
    quantity_id INT NOT NULL,
    FOREIGN KEY (quantity_id) REFERENCES physical_quantity(quantity_id) ON DELETE CASCADE
);

-- Incident Table
CREATE TABLE incident (
    incident_id INT AUTO_INCREMENT PRIMARY KEY,
    responder_id INT,
    responder_comments TEXT,
    resolved_date DATE,
    priority VARCHAR(50),
    FOREIGN KEY (responder_id) REFERENCES user(user_id) ON DELETE SET NULL
);

-- Incident Measurement Bridge Table
CREATE TABLE incident_measurement (
    id INT AUTO_INCREMENT PRIMARY KEY,
    measurement_id INT NOT NULL,
    incident_id INT NOT NULL,
    FOREIGN KEY (measurement_id) REFERENCES measurement(measurement_id) ON DELETE CASCADE,
    FOREIGN KEY (incident_id) REFERENCES incident(incident_id) ON DELETE CASCADE
);

CREATE USER IF NOT EXISTS 'sensor_app'@'localhost' IDENTIFIED BY '165456678';
GRANT SELECT, INSERT, UPDATE, DELETE ON sensor_monitoring.* TO 'sensor_app'@'localhost';
FLUSH PRIVILEGES;
