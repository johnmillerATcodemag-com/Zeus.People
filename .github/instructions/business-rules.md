# Business Rules for Academic Management System
This document outlines the business rules implemented in the Academic Management System, ensuring compliance with the specified requirements.
These rules govern the relationships and constraints between various entities such as Academics, Departments, Rooms, Extensions, and more.
## Key Entities
- **Academic**: Represents faculty members with attributes like empNr, EmpName, Rank, and contract details.
- **Department**: Represents academic departments with attributes like name, budget, and leadership.
- **Room**: Represents physical rooms with unique identifiers.
- **Extension**: Represents communication extensions with access levels.
- **AccessLevel**: Represents different levels of access for academics.
- **Degree**: Represents academic qualifications obtained by academics.
- **University**: Represents institutions from which degrees are obtained.
- **Chair**: Represents leadership positions held by professors.
- **Committee**: Represents committees served by teaching professors.
- **Subject**: Represents subjects taught by academics.

## Example Facts

The Academic with empNr 715 has EmpName 'Adams A'. 
The Academic with empNr 715 works for the Dept named 'Computer Science'. 
The Academic with empNr 715 occupies the Room with roomNr '69-301'. 
The Academic with empNr 715 uses the Extension with extNr '2345'. 
The Extension with extNr '2345' provides the AccessLevel with code 'LOC'. 
The Academic with empNr 715 is contracted till the Date with mdy-code '01/31/95'. 
The Academic with empNr 139 is tenured. 
The Academic with empNr 715 and empname 'Adams A' works for the Dept named 'Computer Science'.
Dept 'Computer Science' has professors in Quantity 5.  
Professor 'Codd EF' holds Chair 'Databases'.  
Professor 'Codd EF' obtained Degree 'BSc' from University 'UQ'. 
Professor 'Codd EF' heads Dept 'Computer Science'. 
Professor 'Codd EF' has HomePhone '965432'. 
Dept 'Computer Science' has senior lecturers in Quantity 9. 
SeniorLecturer 'Hagar TA' obtained Degree 'BInfTech' from University 'UQ'. 
Department 'Computer Science' has lecturers in Quantity 8.  
Lecturer 'Adams A' obtained Degree 'MSc' from University 'OXON'.
Academic 430 has EmpName 'Codd EF'.
Academic 430 has Rank 'P'
Academic 430 holds Chair 'Databases'.
Academic 430 works for Dept 'Computer Science'.


## Reference schemes

    Academic (empNr);    
    Dept (name); 
    Room (roomNr);    
    Extension (extNr); 
    AccessLevel (code);    
    Date (mdy) 

# Business Rules
- A Professor is an Academic who has Rank of 'P'.
- A Professor who heads a Dept must work for that Dept.
- A Teacher that audits another Teacher cannot be audited by that Teacher.
- A TeachingProfessor is both a Teacher and a Professor.
- Academic has Date indicating their contract end.
- Academic has EmpName.
- Academic has one and only one EmpName.
- Academic has one and only one Rank.
- Academic has Rank.
- Academic is tenured.
- Academic obtains Degree from University.
- Academic occupies one and only one Room.
- Academic occupies Room.
- Academic teaches zero or more Subject.
- Academic uses Extension.
- Academic uses one and only one Extension.
- Academic who is tenured must not have a Date indicating their contract end.
- Academic works for Dept.
- AccessLevel has a code of 'LOC', 'INT', or 'NAT'.
- An Academic obtains that Degree from at most one University.
- BldgName is assigned to one and only one Building.
- BldgNr is assigned to one and only one Building.
- Building has BldgName.
- Building has bldgNr.
- Building has one and only one BldgName.
- Building has one and only one bldgNr.
- Building has one or more Room.
- Chair has name.
- Chair has one and only one name.
- Chair is held by at most one Professor.
- Chair is held by at most one Professor.
- Committee has name.
- Committee has one and only one name.
- Committee is served by zero or more TeachingProfessor.
- Degree has code.
- Degree has one and only one code.
- Dept has a most one Chair.
- Dept has Chair.
- Dept has head with home PhoneNr.
- Dept has one and only one head home PhoneNr.
- Dept is headed by at most one Professor.
- Dept must have research budget of MoneyAmt.
- Dept must have teaching budget of MoneyAmt.
- Each Academic that works for a Dept must have a unique EmpName in that Dept.
- empNr is assigned to one and only one Academic.
- Extension is used by one and only one Academic.
- MoneyAmt must be a positive value in US Dollars
- Professor heads at most one Dept.
- Professor heads Dept.
- Professor holds at most one Chair.
- Professor holds Chair.
- Rank ensures AccessLevel.
- Rank ensures one and only one AccessLevel.
- Rank has a code of 'P', 'SL', or 'L'.
- Rating must be a value between 1 and 7.
- Room is in Building.
- Room is in one and only one Building.
- Room is occupied by one or more Academic.
- Subject has code.
- Subject has one and only one code.
- Subject is taught by zero or more Academic.
- Teacher audits Teacher.
- Teacher is an Academic who teaches some Subject.
- Teacher is audited by Teacher.
- Teaching gets a Rating.
- TeachingProfessor serves on Committee.
- TeachingProfessor serves on zero or more Committee.
- The comination of a Room has roomNr and Room is in Building is unique.
- The fact that an Academic teaches a Subject is referred to as a Teaching.
- University has code.
- University has one and only one code.

## Derived Rules
- Dept employs academics of Rank in Quantity iff Quantity = count each Academic who has Rank and works for Dept
- Extension provides AccessLevel as Extension is used by an Academic who has a Rank that ensures AccessLevel
