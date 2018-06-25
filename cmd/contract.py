import json
import web3
import os
import time

from web3 import Web3, HTTPProvider

class Contract:
    abi = None
    bin = None
    
    address = None
    instance = None

    w3 = None

    def load(self, name):
        with open(os.path.join('../bin/', name + '.bin'), 'r') as f:
            self.bin = f.read()
            f.close()

        with open(os.path.join('../bin/', name + '.abi'), 'r') as f:
            self.abi = json.loads(f.read())
            f.close()   


    def deploy(self, sender, _args):
        contract = self.w3.eth.contract(abi=self.abi, bytecode=self.bin)
        time.sleep(1)
        tx_hash = contract.deploy(transaction={'from': sender, 'gas': 4500000, 'value': _args[1]}, args=_args)
        time.sleep(1)

        tx_receipt = self.w3.eth.getTransactionReceipt(tx_hash)
        self.address = tx_receipt['contractAddress']
        self.instance = self.w3.eth.contract(abi=self.abi, address=self.address)


    def from_address(self, address):
        self.address = address
        self.instance = self.w3.eth.contract(abi=self.abi, address=self.address)
    

    def set_w3(self, w3):
        self.w3 = w3


