# Python
import os
import logging
from locust import HttpUser, task, between
from dotenv import load_dotenv

# Load environment variables from .env file if present
load_dotenv()


class ApiUser(HttpUser):
    wait_time = between(1, 3)
    host = "localhost"
    timeout_duration = 90  # seconds

    def on_start(self):
        # Set debug mode from environment variable
        self.ENABLE_LOGGING = os.getenv("ENABLE_LOGGING", "True") == "True"
        # Set up logging
        if self.ENABLE_LOGGING:
            logging.basicConfig(level=logging.DEBUG)
        else:
            logging.basicConfig(level=logging.WARNING)
        # No authentication required as per HTTP Request File

    @task
    def run_scenario(self):
        self.get()

    def get(self):
        """
        GET /
        Content-Type: application/json
        """
        url = "/"
        headers = {"Accept": "application/json"}
        if self.ENABLE_LOGGING:
            print("[Locust] Sending GET / request")

        with self.client.get(
            url=url,
            headers=headers,
            name="GET /",
            catch_response=True,
            timeout=self.timeout_duration,
        ) as response:
            if response.status_code in [200]:
                response.success()
                if self.ENABLE_LOGGING:
                    print("[Locust] GET / succeeded")
            else:
                msg = f"GET / failed with status {response.status_code}, response: {response.text}"
                response.failure(msg)
                if self.ENABLE_LOGGING:
                    logging.error(msg)
                    logging.error(f"Request URL: {response.url}")
                    logging.error(f"Request Headers: {headers}")

    def on_stop(self):
        # No resources to clean up for this scenario
        pass


# To run:
# locust -f locustfile.py -u 10 -r 2 --run-time 1m
