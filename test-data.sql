-- Insert test departments
INSERT INTO Departments (Id, Name, CreatedAt, UpdatedAt) VALUES 
(NEWID(), 'Computer Science', GETUTCDATE(), GETUTCDATE()),
(NEWID(), 'Mathematics', GETUTCDATE(), GETUTCDATE()),
(NEWID(), 'Engineering', GETUTCDATE(), GETUTCDATE());

-- Insert test academics
INSERT INTO Academics (Id, EmpNr, EmpName, Rank, CreatedAt, UpdatedAt) VALUES 
(NEWID(), 'AB1234', 'Dr. John Smith', 'P', GETUTCDATE(), GETUTCDATE()),
(NEWID(), 'CD5678', 'Dr. Jane Doe', 'SL', GETUTCDATE(), GETUTCDATE()),
(NEWID(), 'EF9012', 'Dr. Bob Johnson', 'L', GETUTCDATE(), GETUTCDATE());
