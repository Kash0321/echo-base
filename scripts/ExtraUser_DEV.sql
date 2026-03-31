-- Insertar usuario de prueba
INSERT INTO Users (Id, Name, Email, BusinessLine, EmailNotifications, PhoneNumber, TeamsNotifications)
VALUES ('00000000-0000-0000-0000-000000000002', 'Prueba 001', 'test@localhost', 0, 1, NULL, 0);

-- Insertar roles para el usuario
INSERT INTO UserRole (RolesId, UsersId)
VALUES ('D0000000-0000-0000-0000-000000000002', '00000000-0000-0000-0000-000000000002');

INSERT INTO UserRole (RolesId, UsersId)
VALUES ('D0000000-0000-0000-0000-000000000003', '00000000-0000-0000-0000-000000000002');