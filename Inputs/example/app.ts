#compiler build("alpha", "swift", "ios");

#compiler addDependency("Stripe");

import "Stripe";
Stripe.setDefaultPublishableKey("pk_test_TYooMQauvdEDq54NiTphI7jx");
