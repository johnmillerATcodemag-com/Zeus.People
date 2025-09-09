# Test Coverage Improvement Action Plan

## Current Status: 37.4% Coverage (❌ Below 80% requirement)

### Immediate Actions to Improve Coverage

#### 1. Add Missing Command Handler Tests

- **SetAcademicTenureCommandHandler** (Currently 0%)
- **UpdateAcademicCommandHandler** (Currently 0%)
- **DeleteAcademicCommandHandler** (Currently 0%)
- **Department command handlers** (Mixed 0-100%)
- **Extension command handlers** (All 0%)
- **Room command handlers** (All 0%)

#### 2. Complete Infrastructure Testing

- **Repository implementations** (Database integration)
- **EF Core context testing**
- **Configuration services**
- **Health checks**

#### 3. Add Security & Authentication Tests

- **JWT authentication** (Currently 0%)
- **Authorization policies**
- **Security middleware**

#### 4. API Integration Tests

- **Controller integration tests**
- **Request/response validation**
- **Error handling scenarios**

### Quick Win Example: Academic Command Handler Tests

Here's an example of tests that could be added to immediately improve coverage:

```csharp
// Add to Zeus.People.Application.Tests
[Test]
public async Task SetAcademicTenureCommandHandler_Should_Set_Tenure_Successfully()
{
    // Arrange
    var command = new SetAcademicTenureCommand("EMP001", true);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();
}

[Test]
public async Task UpdateAcademicCommandHandler_Should_Update_Academic_Successfully()
{
    // Arrange
    var command = new UpdateAcademicCommand("EMP001", "Updated Name", "P");

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();
}
```

### Estimated Coverage Impact

| Action                 | Estimated Coverage Increase | Effort Level       |
| ---------------------- | --------------------------- | ------------------ |
| Command Handler Tests  | +15%                        | Low                |
| Infrastructure Tests   | +20%                        | Medium             |
| Security Tests         | +8%                         | Medium             |
| API Integration Tests  | +10%                        | Low                |
| **Total Potential**    | **+53%**                    | **Medium**         |
| **Projected Coverage** | **90.4%**                   | **✅ Exceeds 80%** |

### Implementation Timeline

#### Week 1: Command Handlers (+15%)

- Complete all missing command handler tests
- Add validation and error scenario tests
- Target: 52% coverage

#### Week 2: API Integration Tests (+10%)

- Add comprehensive controller tests
- Include authentication scenarios
- Target: 62% coverage

#### Week 3: Infrastructure (+20%)

- Repository integration tests
- Database connection tests
- Configuration validation tests
- Target: 82% coverage

#### Week 4: Security & Polish (+8%)

- Authentication/authorization tests
- Error handling completeness
- Performance test integration
- Target: 90%+ coverage

### Next Steps

1. **Run current coverage analysis**: ✅ Completed
2. **Identify critical gaps**: ✅ Completed
3. **Create test implementation plan**: ✅ Completed
4. **Begin command handler test development**: 🔄 Ready to start
5. **Add infrastructure tests**: ⏳ Pending
6. **Implement security tests**: ⏳ Pending
7. **Validate 80%+ coverage**: ⏳ Pending

### Success Criteria

- [ ] Overall line coverage > 80%
- [ ] Branch coverage > 70%
- [ ] All critical business logic covered
- [ ] Security scenarios tested
- [ ] Infrastructure integration validated
- [ ] Performance benchmarks established

**Conclusion**: The 80% coverage requirement is achievable within 3-4 weeks of focused testing development, with the most immediate impact coming from completing the missing command handler tests.
