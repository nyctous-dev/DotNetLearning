# Unit Testing with .NET

## Micro Goal

Learn how to write and run automated unit tests in .NET using xUnit, verify expected behavior with assertions, isolate the code under test, and use tests to catch regressions as an application evolves.

## Concepts Covered

- Unit testing and the purpose of automated tests
- Test projects versus application projects
- xUnit and the .NET test runner
- Test methods and test naming
- Arrange, Act, Assert (AAA)
- Assertions with `Assert`
- Testing return values and state changes
- Testing exceptions
- Parameterized tests with `[Theory]` and `[InlineData]`
- Testing boundary conditions and invalid input
- Separating application logic from console output
- Test independence and repeatability
- Running tests with the .NET CLI
- Understanding test failures and debugging
- Basic test coverage and its limitations

## Completion Criteria

- [ ] Explain the difference between a unit test and an application run.
- [ ] Create a dedicated xUnit test project.
- [ ] Reference the application or class library being tested.
- [ ] Write test methods using the Arrange, Act, Assert pattern.
- [ ] Verify results using assertions.
- [ ] Test normal inputs, boundary conditions, and invalid inputs.
- [ ] Write parameterized tests using `[Theory]` and `[InlineData]`.
- [ ] Verify that code throws the expected exception when appropriate.
- [ ] Run tests from the command line using `dotnet test`.
- [ ] Diagnose a failing test and correct either the implementation or the test.
- [ ] Keep tests independent of execution order and external state.
- [ ] Explain why passing tests provide evidence of correctness but do not prove that a program is entirely bug-free.

## Lecture

### 1. What unit testing is

A unit test is an automated check that verifies a small, specific piece of code behaves as expected. A unit is commonly a method or a small class, tested with its dependencies controlled where necessary.

Suppose a Boids simulation calculates the distance between two points. For coordinates `(0, 0)` and `(3, 4)`, the expected distance is `5`.

You could run the application and inspect the printed result manually. A unit test checks that expectation automatically:

```csharp
double distance = Geometry.CalculateDistance(0, 0, 3, 4);

Assert.Equal(5.0, distance);
```

If the actual result differs from the expected value, the test fails. When the implementation changes later, you can rerun the test to check that the behavior still works.

Unit tests are useful because they provide fast feedback, document expected behavior, and help detect regressions—bugs introduced when existing code is modified.

### 2. Application projects and test projects

An application project contains the code that performs the program's work. A test project contains automated tests that exercise that code.

These projects have different purposes. A console application has an entry point and runs as a program. A test project is discovered and executed by a test runner.

A common repository structure is:

```text
10-UnitTestingWithDotNet/
├── README.md
├── BoidLogic/
│   ├── BoidLogic.csproj
│   └── Geometry.cs
└── BoidLogic.Tests/
    ├── BoidLogic.Tests.csproj
    └── GeometryTests.cs
```

This example uses a small class library as the code under test. The test project references that library.

Your learning repository already contains a directory named `10-UnitTestingWithDotNet`. Treat it as the module's learning area, not necessarily as the final test project itself. A dedicated test project should use the appropriate test template and test framework.

For example, from the `02-dotnet-fundamentals` directory, you can create an xUnit test project with:

```bash
dotnet new xunit -n 10-UnitTestingWithDotNet.Tests
```

If you are testing a separate class library, add a project reference from the test project to that library:

```bash
dotnet add 10-UnitTestingWithDotNet.Tests/10-UnitTestingWithDotNet.Tests.csproj reference path/to/BoidLogic.csproj
```

Replace `path/to/BoidLogic.csproj` with the actual path to the project containing the code being tested. Do not use the example path literally.

The xUnit template configures the test project with the packages and settings needed for test discovery and execution. Exact package versions depend on the installed SDK and template. You do not need to add those dependencies manually when the template already includes them.

### 3. The test runner and `dotnet test`

The .NET CLI provides commands for building and testing projects.

Common commands include:

```bash
dotnet restore
dotnet build
dotnet test
```

- `dotnet restore` resolves and downloads the project's dependencies.
- `dotnet build` compiles the project and its dependencies.
- `dotnet test` builds and runs tests using the configured test infrastructure.

To test one specific project, pass its project file:

```bash
dotnet test path/to/BoidLogic.Tests.csproj
```

You can also run tests from a solution file or a directory containing a solution. For a repository with multiple independent projects, specifying the test project avoids accidentally running a different project.

A successful test run reports the number of passed, failed, and skipped tests. A build failure is different from a test failure: a build failure means the code could not compile, while a test failure means a test ran but its expected result did not match the observed behavior.

### 4. Writing your first unit test

Consider a class library that exposes a method for calculating distance.

```csharp
namespace BoidLogic;

public static class Geometry
{
    public static double CalculateDistance(
        double x1,
        double y1,
        double x2,
        double y2)
    {
        double deltaX = x2 - x1;
        double deltaY = y2 - y1;

        return Math.Sqrt(
            deltaX * deltaX + deltaY * deltaY);
    }
}
```

A corresponding xUnit test can verify a known result:

```csharp
using BoidLogic;
using Xunit;

namespace BoidLogic.Tests;

public class GeometryTests
{
    [Fact]
    public void CalculateDistance_ReturnsFive_ForThreeFourFiveTriangle()
    {
        // Arrange
        double x1 = 0.0;
        double y1 = 0.0;
        double x2 = 3.0;
        double y2 = 4.0;

        // Act
        double result = Geometry.CalculateDistance(
            x1, y1, x2, y2);

        // Assert
        Assert.Equal(5.0, result);
    }
}
```

The `[Fact]` attribute marks a test that runs once with its explicitly supplied setup. The test runner discovers the method and executes it.

The method name describes the behavior being checked. A useful naming pattern is:

`MethodName_ExpectedBehavior_Condition`

The name does not affect test behavior, but a descriptive name makes failures easier to understand.

### 5. Arrange, Act, Assert

The Arrange, Act, Assert (AAA) pattern divides a test into three logical parts.

- **Arrange:** Prepare the inputs and any required objects.
- **Act:** Call the method or operation being tested.
- **Assert:** Verify the observed result.

In the distance example, the coordinates are arranged, the calculation is performed, and the result is compared with the expected value.

AAA is a convention, not a requirement enforced by the compiler. A very small test may not need three visibly separated sections. The important point is to keep the test's purpose clear and avoid mixing setup, execution, and verification in a confusing way.

A unit test should generally verify one behavior or one closely related set of expectations. If a test checks many unrelated behaviors, a failure becomes harder to diagnose.

### 6. Assertions

Assertions compare the actual behavior with an expectation. If an assertion fails, the test runner reports a test failure.

Common xUnit assertions include:

```csharp
Assert.Equal(10, actual);
Assert.NotEqual(10, actual);

Assert.True(condition);
Assert.False(condition);

Assert.Null(value);
Assert.NotNull(value);

Assert.Contains(expectedItem, collection);
Assert.Empty(collection);
Assert.Single(collection);
```

Use assertions that express the requirement directly. For example, `Assert.Empty(collection)` communicates the expectation more clearly than asserting that the collection's count equals zero.

For floating-point calculations, exact equality can be inappropriate when rounding or accumulated numerical error is expected. In those cases, use an appropriate precision or tolerance:

```csharp
Assert.Equal(expected, actual, precision: 6);
```

This overload compares decimal precision. For numerical algorithms where an absolute error bound is more meaningful, use an explicit tolerance:

```csharp
double tolerance = 1e-9;

Assert.True(
    Math.Abs(expected - actual) < tolerance);
```

Choose the tolerance based on the algorithm and expected scale. Do not choose a tolerance so large that it allows incorrect results to pass.

### 7. Test more than the typical input

A single successful example does not establish that a method behaves correctly for every relevant input.

For the distance calculation, useful cases include:

- Two identical points, where the distance should be zero.
- A horizontal displacement.
- A vertical displacement.
- A diagonal displacement.
- Negative coordinates.
- Very large or invalid values, if the method's contract addresses them.

A test should reflect the method's intended contract. If the method is expected to support negative coordinates, those coordinates are valid test inputs rather than invalid data.

Boundary testing is particularly important. Bugs frequently appear at zero, minimum and maximum values, empty collections, or transitions between valid and invalid input.

### 8. Parameterized tests with `[Theory]`

When the same behavior must be checked against several inputs, xUnit provides `[Theory]` and `[InlineData]`.

```csharp
using BoidLogic;
using Xunit;

namespace BoidLogic.Tests;

public class GeometryTests
{
    [Theory]
    [InlineData(0.0, 0.0, 0.0, 0.0, 0.0)]
    [InlineData(0.0, 0.0, 3.0, 4.0, 5.0)]
    [InlineData(1.0, 2.0, 1.0, 7.0, 5.0)]
    [InlineData(-1.0, -1.0, 2.0, 3.0, 5.0)]
    public void CalculateDistance_ReturnsExpectedResult(
        double x1,
        double y1,
        double x2,
        double y2,
        double expected)
    {
        double actual = Geometry.CalculateDistance(
            x1, y1, x2, y2);

        Assert.Equal(expected, actual);
    }
}
```

Each `[InlineData]` row supplies a separate set of arguments. The test runner executes the theory for each row.

Parameterized tests reduce duplicated setup and make it easier to expand coverage. Use them when several cases verify the same underlying behavior. Keep separate tests when the cases represent meaningfully different behaviors or require different assertions.

### 9. Testing exceptions

Sometimes the correct behavior is to reject invalid input. If a method's contract requires an exception, test for that exception explicitly.

For example, imagine a `Boid` constructor rejects negative speeds by throwing `ArgumentOutOfRangeException`. A test can verify that behavior:

```csharp
using Xunit;

namespace BoidLogic.Tests;

public class BoidTests
{
    [Fact]
    public void Constructor_RejectsNegativeSpeed()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Boid(
                id: 1,
                x: 0.0,
                y: 0.0,
                speed: -1.0));
    }
}
```

This example assumes a `Boid` class with the specified constructor exists in the test project's referenced code.

`Assert.Throws<T>` verifies that the expected exception type is thrown by the supplied action. If no exception is thrown, or a different exception is thrown, the test fails.

Do not write tests that merely expect any exception. Verify the specific exception that represents the documented behavior. When useful, also check details such as the relevant parameter name or error message.

### 10. Test behavior, not implementation details

A test should normally verify what a method promises to do, rather than how its code happens to accomplish that result.

For example, a test of `CalculateDistance` should verify the returned distance. It should not depend on whether the implementation uses intermediate variables, a particular loop, or a specific mathematical expression.

Implementation-focused tests can break when the code is refactored even if the externally observable behavior remains correct. Behavior-focused tests provide more freedom to improve the implementation.

For a Boids simulation, useful unit-test targets include:

- Distance calculations.
- Speed and position validation.
- Movement updates.
- Steering calculations.
- Boundary handling.
- Collection operations with well-defined behavior.

The user interface, rendering loop, and full simulation behavior may require other forms of testing. Unit tests are one part of a broader testing strategy.

### 11. Keep code testable

Code that mixes calculations, console input, console output, file access, and application startup in one block can be difficult to test independently.

A useful approach is to put reusable logic in methods or classes and keep application startup focused on coordinating those components.

For example, instead of having a method both calculate a value and print it, return the value and let the caller decide how to display it:

```csharp
public static double CalculateSpeed(
    double baseSpeed,
    double multiplier)
{
    return baseSpeed * multiplier;
}
```

This method can be tested without redirecting console output or launching an interactive session.

When code depends on external services, files, clocks, or other changing resources, you may need to isolate those dependencies. Dependency injection and interfaces can help, but do not introduce abstractions without a reason. Start with simple, deterministic methods whenever possible.

### 12. Test independence and repeatability

A good unit test should produce the same result when run repeatedly under the same conditions. It should not depend on another test running first.

Avoid relying on shared mutable state between tests. Each test should arrange the state it needs and clean up any resources it creates.

Tests that depend on network availability, real databases, file paths on one machine, or current time can become unreliable. These may be valid integration tests, but they should be treated differently from isolated unit tests.

A test suite should also run without requiring manual input. If a test needs a specific file, create or arrange that file as part of the test setup rather than assuming it already exists on the developer's machine.

### 13. Test failures and debugging

When a test fails, examine:

1. The name of the failing test.
2. The assertion that failed.
3. The expected and actual values.
4. The inputs used by the test.
5. Whether the implementation or the test's expectation is incorrect.

Do not automatically change an expected value just to make a test pass. The expected result should come from the method's requirements or a reliable independent calculation.

A failing test can reveal a bug in the application, an incorrect test, or a misunderstanding of the required behavior. Diagnose the cause before making a change.

After fixing the issue, rerun the relevant test and then the broader test suite. This helps ensure that the fix has not broken another behavior.

### 14. Test coverage and its limitations

Code coverage measures which portions of code were executed while tests ran. It can help identify code that has not been exercised, but high coverage does not guarantee correct behavior.

A line can execute without its result being checked. A test can also execute a branch without verifying that the branch produces the right outcome.

Focus on meaningful assertions and important behavior, especially:

- Core calculations.
- Boundary conditions.
- Invalid input.
- Error handling.
- State changes.
- Important interactions between components.

A small set of well-designed tests is more valuable than a large set of tests that merely execute code without checking its behavior.

## Challenge

Create a small, testable Boids logic component and a separate xUnit test project that verifies its behavior.

Your solution must satisfy all of the following requirements:

1. **Create a testable component:** Implement a `Boid` class or a small `BoidMovement` class in a project that can be referenced by the test project. Keep the logic independent of console input and output.
2. **Implement movement:** Provide a method that updates a boid's position using supplied X and Y movement offsets.
3. **Validate speed:** Ensure that negative movement speeds are rejected with an appropriate exception.
4. **Create a test project:** Use the xUnit test template and add a project reference to the code being tested.
5. **Test normal behavior:** Verify that movement updates the expected coordinates.
6. **Test independent instances:** Create two boids, move one, and verify that the other object's state is unchanged.
7. **Test invalid input:** Verify that constructing a boid with a negative speed throws the expected exception.
8. **Use parameterized tests:** Write a `[Theory]` with multiple `[InlineData]` cases for a deterministic calculation, such as calculating distance or movement speed.
9. **Use meaningful assertions:** Verify actual values and behavior rather than merely asserting that a method completed without throwing.
10. **Run the suite:** Execute the tests with `dotnet test` and confirm that they pass.
11. **Diagnose a failure:** Intentionally change one expected result, run the tests, inspect the failure, restore the correct expectation, and run the suite again.
12. **Keep tests independent:** Each test must arrange the objects and values it needs without relying on another test's execution order.


dotnet new classlib -n BoidLogic -o BoidLogic
dotnet new xunit -n BoidLogic.Tests -o BoidLogic.Tests
dotnet add BoidLogic.Tests/BoidLogic.Tests.csproj reference BoidLogic/BoidLogic.csproj
