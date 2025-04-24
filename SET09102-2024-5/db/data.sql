USE sensor_monitoring;

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
    (SELECT role_id FROM role WHERE role_name = 'Administrator'), 
    access_privilege_id 
FROM 
    access_privilege
ON DUPLICATE KEY UPDATE role_id = role_id;

-- Assign privileges for Environmental Scientist
INSERT INTO role_privilege (role_id, access_privilege_id)
SELECT 
    (SELECT role_id FROM role WHERE role_name = 'Environmental Scientist'), 
    access_privilege_id 
FROM 
    access_privilege 
WHERE 
    name IN ('sensor.view', 'data.view', 'data.analyze', 'data.export', 
             'alert.view', 'alert.respond', 'report.view', 'report.create', 'report.export')
ON DUPLICATE KEY UPDATE role_id = role_id;

-- Assign privileges for Operations Manager
INSERT INTO role_privilege (role_id, access_privilege_id)
SELECT 
    (SELECT role_id FROM role WHERE role_name = 'Operations Manager'), 
    access_privilege_id 
FROM 
    access_privilege 
WHERE 
    name IN ('sensor.view', 'sensor.edit', 'data.view', 
             'alert.view', 'alert.respond', 'alert.configure',
             'maintenance.view', 'maintenance.schedule', 'report.view')
ON DUPLICATE KEY UPDATE role_id = role_id;

-- Assign privileges for Field Technician
INSERT INTO role_privilege (role_id, access_privilege_id)
SELECT 
    (SELECT role_id FROM role WHERE role_name = 'Field Technician'), 
    access_privilege_id 
FROM 
    access_privilege 
WHERE 
    name IN ('sensor.view', 'maintenance.view', 'maintenance.perform')
ON DUPLICATE KEY UPDATE role_id = role_id;

-- Assign limited privileges to Guest role
INSERT INTO role_privilege (role_id, access_privilege_id)
SELECT 
    (SELECT role_id FROM role WHERE role_name = 'Guest'), 
    access_privilege_id 
FROM 
    access_privilege 
WHERE 
    name IN ('sensor.view', 'data.view')
ON DUPLICATE KEY UPDATE role_id = role_id;

-- Ensure at least one administrator user exists
INSERT INTO user (first_name, last_name, email, role_id, password_hash, password_salt)
SELECT 'Admin', 'User', 'admin@example.com', 
       (SELECT role_id FROM role WHERE role_name = 'Administrator'),
       -- Pre-hashed password for 'admin123'
       'QupYj2NpJbHnprPi9U3Y25TWx9kpNsS9mUUkWRbMr+E=',
       'MIAvLJtBvsvt4gw0xZogLHdgTMXKGGvS3F6Do7+Isi7PUP2+5rcPA7Q/QE8Pt1Vig+3XwXxyuWWZh5Ex3ynVg0hZyqnsA3KhXVKPpJGaZvBLGdvXivRskmTFI4SS0VGyHj4Q9KDbI+GQeFbD3XWbBGZ1+FZGcJbDKBMjNSIsRPc='
FROM dual 
WHERE NOT EXISTS(SELECT 1 FROM user WHERE email = 'admin@example.com');

-- Ensure we have a test user for each role
INSERT INTO user (first_name, last_name, email, role_id, password_hash, password_salt)
SELECT 'Scientist', 'User', 'scientist@example.com', 
       (SELECT role_id FROM role WHERE role_name = 'Environmental Scientist'),
       -- Pre-hashed password for 'password'
       'QupYj2NpJbHnprPi9U3Y25TWx9kpNsS9mUUkWRbMr+E=',
       'MIAvLJtBvsvt4gw0xZogLHdgTMXKGGvS3F6Do7+Isi7PUP2+5rcPA7Q/QE8Pt1Vig+3XwXxyuWWZh5Ex3ynVg0hZyqnsA3KhXVKPpJGaZvBLGdvXivRskmTFI4SS0VGyHj4Q9KDbI+GQeFbD3XWbBGZ1+FZGcJbDKBMjNSIsRPc='
FROM dual 
WHERE NOT EXISTS(SELECT 1 FROM user WHERE email = 'scientist@example.com');

INSERT INTO user (first_name, last_name, email, role_id, password_hash, password_salt)
SELECT 'Operations', 'Manager', 'operations@example.com', 
       (SELECT role_id FROM role WHERE role_name = 'Operations Manager'),
       -- Pre-hashed password for 'password'
       'QupYj2NpJbHnprPi9U3Y25TWx9kpNsS9mUUkWRbMr+E=',
       'MIAvLJtBvsvt4gw0xZogLHdgTMXKGGvS3F6Do7+Isi7PUP2+5rcPA7Q/QE8Pt1Vig+3XwXxyuWWZh5Ex3ynVg0hZyqnsA3KhXVKPpJGaZvBLGdvXivRskmTFI4SS0VGyHj4Q9KDbI+GQeFbD3XWbBGZ1+FZGcJbDKBMjNSIsRPc='
FROM dual 
WHERE NOT EXISTS(SELECT 1 FROM user WHERE email = 'operations@example.com');

INSERT INTO user (first_name, last_name, email, role_id, password_hash, password_salt)
SELECT 'Tech', 'Support', 'technician@example.com', 
       (SELECT role_id FROM role WHERE role_name = 'Field Technician'),
       -- Pre-hashed password for 'password'
       'QupYj2NpJbHnprPi9U3Y25TWx9kpNsS9mUUkWRbMr+E=',
       'MIAvLJtBvsvt4gw0xZogLHdgTMXKGGvS3F6Do7+Isi7PUP2+5rcPA7Q/QE8Pt1Vig+3XwXxyuWWZh5Ex3ynVg0hZyqnsA3KhXVKPpJGaZvBLGdvXivRskmTFI4SS0VGyHj4Q9KDbI+GQeFbD3XWbBGZ1+FZGcJbDKBMjNSIsRPc='
FROM dual 
WHERE NOT EXISTS(SELECT 1 FROM user WHERE email = 'technician@example.com');
