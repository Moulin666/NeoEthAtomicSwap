from logzero import logger
from neo.Core.Blockchain import Blockchain
from time import sleep
from logic import Logic
import node
import threading
import sys

help_text = """
Utility have several commands:

    initiate <participant address> <amount> <secret> <timeout>
    Initiate atomic swap: deploy curret contract and feel correct value.
        <participant address> - address participant for atomic swap.
        <amount> - amount exchange coin value.
        <secret> - secret, when swap initializate have been hashed to sha256 hash.
        <timeout> - timeout in seconds for swap. 

    redeem <contract address> <secret>
    Publish secret, if secret correct, transfer funds.
        <contract address> - address contract atomic swap.
        <secret> - swap.

    refund <contract address>
    Transfer funds back from atomic swap contract, if timeout has expired.
        <contract address> - address contract atomic swap.
"""



def print_help():
    print(help_text)


def main_thread():
    print("Welcome to the Atomic Swap application") 
    if len(sys.argv) == 1:
        print_help()
    elif sys.argv[1] == "initiate":
        Logic().initiate([sys.argv[2],sys.argv[3], sys.argv[4], sys.argv[5]])
    elif sys.argv[1] == "redeem":
        Logic().redeem(sys.argv[2], sys.argv[3])
    elif sys.argv[1] == "refund":
        Logic().refund(sys.argv[2])
    elif sys.argv[1] == "test":
        Logic().Test()
    else:
        print_help()



def main():
    node.start_node()
        
    


if __name__ == "__main__":
    main()

