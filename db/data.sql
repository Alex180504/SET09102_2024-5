-- Add sample users

INSERT INTO role (role_name) VALUES
('operations_manager'),
('administrator'),
('env_scientist');

INSERT INTO user (first_name, last_name, email, role_id) VALUES
('John', 'Manager', 'john.manager@example.com', 1),
('Sarah', 'Admin', 'sarah.admin@example.com', 2),
('Michael', 'Scientist', 'michael.scientist@example.com', 3);
