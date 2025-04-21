DROP DATABASE IF EXISTS sensor_monitoring;

CREATE DATABASE IF NOT EXISTS sensor_monitoring;
USE sensor_monitoring;

-- Role Table
CREATE TABLE role (
    role_id INT AUTO_INCREMENT PRIMARY KEY,
    role_name VARCHAR(100) NOT NULL
);

-- User Table
CREATE TABLE user (
    user_id INT AUTO_INCREMENT PRIMARY KEY,
    first_name VARCHAR(100),
    last_name VARCHAR(100),
    email VARCHAR(255),
    role_id INT,
    password_hash VARCHAR(255),
    password_salt VARCHAR(255),
    FOREIGN KEY (role_id) REFERENCES role(role_id) ON DELETE SET NULL
);

-- Measurand Table
CREATE TABLE measurand (
    measurand_id INT AUTO_INCREMENT PRIMARY KEY,
    quantity_type VARCHAR(100),
    quantity_name VARCHAR(100),
    symbol VARCHAR(20),
    unit VARCHAR(50)
);

-- Sensor Table
CREATE TABLE sensor (
    sensor_id INT AUTO_INCREMENT PRIMARY KEY,
    sensor_type VARCHAR(100),
    status VARCHAR(50),
    deployment_date DATE,
    measurand_id INT NOT NULL,
    FOREIGN KEY (measurand_id) REFERENCES measurand(measurand_id) ON DELETE CASCADE
);

-- Configuration Table
CREATE TABLE configuration (
    sensor_id INT AUTO_INCREMENT PRIMARY KEY,
    latitude FLOAT,
    longitude FLOAT,
    altitude FLOAT,
    orientation INT,
    measurment_frequency INT,
    min_threshold FLOAT,
    max_threshold FLOAT,
    FOREIGN KEY (sensor_id) REFERENCES sensor(sensor_id) ON DELETE CASCADE
);

-- Sensor Firmware Table
CREATE TABLE sensor_firmware (
    sensor_id INT AUTO_INCREMENT PRIMARY KEY,
    firmware_version VARCHAR(50),
    last_update_date DATE,
    FOREIGN KEY (sensor_id) REFERENCES sensor(sensor_id) ON DELETE CASCADE
);

-- Measurement Table 
CREATE TABLE measurement (
    measurement_id INT AUTO_INCREMENT PRIMARY KEY,
    timestamp DATETIME,
    value FLOAT,
    sensor_id INT NOT NULL,
    FOREIGN KEY (sensor_id) REFERENCES sensor(sensor_id) ON DELETE CASCADE
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
    measurement_id INT,
    incident_id INT,
    PRIMARY KEY (measurement_id, incident_id),
    FOREIGN KEY (measurement_id) REFERENCES measurement(measurement_id) ON DELETE CASCADE,
    FOREIGN KEY (incident_id) REFERENCES incident(incident_id) ON DELETE CASCADE
);

-- Maintenance Table
CREATE TABLE maintenance (
    maintenance_id INT AUTO_INCREMENT PRIMARY KEY,
    maintenance_date DATE,
    maintainer_id INT NOT NULL,
    sensor_id INT NOT NULL,
    maintainer_comments TEXT,
    FOREIGN KEY (maintainer_id) REFERENCES user(user_id) ON DELETE CASCADE,
    FOREIGN KEY (sensor_id) REFERENCES sensor(sensor_id) ON DELETE CASCADE
);

CREATE USER IF NOT EXISTS 'sensor_app'@'localhost' IDENTIFIED BY '165456678';
GRANT SELECT, INSERT, UPDATE, DELETE ON sensor_monitoring.* TO 'sensor_app'@'localhost';
FLUSH PRIVILEGES;