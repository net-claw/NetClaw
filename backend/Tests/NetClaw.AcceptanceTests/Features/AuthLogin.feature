Feature: Auth login

This feature verifies that an admin can create a new user and that the new user can authenticate.

Scenario: Newly created user can login
    Given the acceptance test client is ready
    And I login with the seeded admin account
    And I create a new active user
    And I logout
    When I login with the new user
    Then the response status should be OK
