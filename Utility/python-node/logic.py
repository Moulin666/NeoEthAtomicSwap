import argparse
import datetime
import json
import os
import psutil
import traceback
import logging
import sys
import json
import hashlib

from time import sleep
from logzero import logger
from prompt_toolkit import prompt
from prompt_toolkit.contrib.completers import WordCompleter
from prompt_toolkit.history import FileHistory
from prompt_toolkit.shortcuts import print_tokens
from prompt_toolkit.styles import style_from_dict
from prompt_toolkit.token import Token
from twisted.internet import reactor, task

from neo import __version__
from neo.Core.Blockchain import Blockchain
from neocore.Fixed8 import Fixed8
from neo.IO.MemoryStream import StreamManager
from neo.Wallets.utils import to_aes_key
from neo.Implementations.Blockchains.LevelDB.LevelDBBlockchain import LevelDBBlockchain
from neo.Implementations.Blockchains.LevelDB.DebugStorage import DebugStorage
from neo.Implementations.Wallets.peewee.UserWallet import UserWallet
from neo.Implementations.Notifications.LevelDB.NotificationDB import NotificationDB
from neo.Network.NodeLeader import NodeLeader
from neo.Prompt.Commands.BuildNRun import BuildAndRun, LoadAndRun
from neo.Prompt.Commands.Invoke import InvokeContract, TestInvokeContract, test_invoke
from neo.Prompt.Commands.LoadSmartContract import *
from neo.Prompt.Commands.Send import construct_and_send, parse_and_sign
from neo.contrib.nex.withdraw import RequestWithdrawFrom, PrintHolds, DeleteHolds, WithdrawOne, WithdrawAll, \
    CancelWithdrawalHolds, ShowCompletedHolds, CleanupCompletedHolds
from neo.Prompt.Commands.Tokens import token_approve_allowance, token_get_allowance, token_send, token_send_from, \
    token_mint, token_crowdsale_register
from neo.Prompt.Commands.Wallet import DeleteAddress, ImportWatchAddr, ImportToken, ClaimGas, DeleteToken, AddAlias, \
    ShowUnspentCoins
from neo.Prompt.Utils import get_arg, get_from_addr
from neo.Prompt.InputParser import InputParser
from neo.Settings import settings, PrivnetConnectionError, PATH_USER_DATA
from neo.UserPreferences import preferences
from neocore.KeyPair import KeyPair
from neocore.UInt256 import UInt256
from neo.bin.prompt import PromptInterface
from neo.Implementations.Wallets.peewee.UserWallet import UserWallet
from neo.contrib.smartcontract import SmartContract
from neo.Core.State.ContractState import ContractState


class Logic (PromptInterface):
    hash
    

    def initiate(self, args):
        # Load or create wallet.
        arg = ('wallet', 'walletfile')
        if os.path.exists("walletfile"):
                    self.do_open(arg)
        else:
            self.do_create(arg)
        # Wait for Gas transfer.
        print("Please transfer 1000 Gas to this addres: " + self.Wallet.Addresses[0])
        input("Please write yes if funds transfer complited: ")
        sleep(20)
        '''
        tmp = tmp[0].split(' ')
        tmp = tmp[1].split('.')
        balance = tmp[0]
        if (int(balance) >= int(args[1])):
        '''
        # Open settings file.
        with open('settings.json') as f:
                data = json.load(f)
                # Build contract arguments.
                contract_args = ('contract', data['contract_path'], data['params'], data['returntype'], data['needs_storage'], data['needs_dynamic_invoke'])
                # Public contract to the blockchain and get HASH of contract.
                hash = self.load_smart_contract(contract_args)
                print("HASH: " + hash )
                # Getting public key of the wallet addres. 
                data = self.Wallet.ToJson()
                tmp = data['public_keys'][0]
                pub_key = tmp['Public Key'] 
                sleep(60)
                # Mint atomic token to the initiater of swap.
                invoke_args = [hash,"MintTokens",[bytearray(pub_key.encode('utf-8')),bytearray(pub_key.encode('utf-8')),bytearray(args[1].encode('utf-8'))]]
                self.test_invoke_contract(invoke_args) 
                sleep(60)
                # Initiate(Deploy) swap to the blockchain.
                invoke_args = [hash,"Deploy",[bytearray(pub_key.encode('utf-8')),bytearray(args[0].encode('utf-8')),bytearray(args[1].encode('utf-8')),bytearray(hashlib.sha256(args[2].encode('utf-8')).digest()),bytearray(args[3].encode('utf-8'))]]
                self.test_invoke_contract(invoke_args) 
                sleep(60)  
                self.quit()
                print("initiate")
        

    def redeem(self,contract_address, secret):
        # Load or create wallet.
        arg = ('wallet', 'walletfile')
        if os.path.exists("walletfile"):
                    self.do_open(arg)
        else:
            self.do_create(arg)
        # Getting public key of the wallet addres. 
        data = self.Wallet.ToJson()
        tmp = data['public_keys'][0]
        pub_key = tmp['Public Key'] 
        # Invoke contract to redeem atomic swap.
        invoke_args = [contract_address,"Redeem",[bytearray(pub_key.encode('utf-8')),bytearray(secret.encode('utf-8'))]]
        self.test_invoke_contract(invoke_args)
        print("redeem")
        sleep(60)  
        self.quit()

    def refund(self,contract_address):
        # Load or create wallet.
        arg = ('wallet', 'walletfile')
        if os.path.exists("walletfile"):
                    self.do_open(arg)
        else:
            self.do_create(arg)
        # Getting public key of the wallet addres. 
        data = self.Wallet.ToJson()
        tmp = data['public_keys'][0]
        pub_key = tmp['Public Key'] 
        # invoke contract to Refund atomic swap.
        invoke_args = [contract_address,"Refund",[bytearray(pub_key.encode('utf-8'))]]
        self.test_invoke_contract(invoke_args)
        print("refund")
        sleep(60)  
        self.quit()

    def Test(self):
        # Load or create wallet.
        arg = ('wallet', 'walletfile')
        if os.path.exists("walletfile"):
            self.do_open(arg)
        byte_array =   bytearray(hashlib.sha256("secret".encode('utf-8')).digest())  
        print("Type of byte_array: "+ str(type(byte_array)))
        byte_array_2 = bytearray("secret".encode('utf-8'))
        invoke_args = ["0x4a1f719abfb17fe22a0862d2b3c5b600b10fca15","Test",[byte_array_2,byte_array]]
        self.test_invoke_contract(invoke_args)
        '''
        data = self.Wallet.ToJson()
        tmp = data['public_keys'][0]
        pub_key = tmp['Public Key'] 
        print(data)'''
        self.quit()
        
    
    # Method from np-prompt but a litlebit edited.
    def load_smart_contract(self, args):
        if not self.Wallet:
            print("Please open a wallet")
            return

        args, from_addr = get_from_addr(args)

        function_code = LoadContract(args[1:])

        if function_code:
            # Fill contract info and generate bytescript.
            contract_script = self.GatherContractDetails(function_code)
            contract_script_json = function_code.ToJson()
            hash = contract_script_json['hash']

            if contract_script is not None:
                # testing invoke contract.
                tx, fee, results, num_ops = test_invoke(contract_script, self.Wallet, [], from_addr=from_addr)

                if tx is not None and results is not None:
                    print("Enter your password to continue and deploy this contract")
                    passwd = prompt("[password]> ", is_password=True)
                    if not self.Wallet.ValidatePassword(passwd):
                        return print("Incorrect password")
                    # Deploy contract to the blockchain.
                    result = InvokeContract(self.Wallet, tx, Fixed8.Zero(), from_addr=from_addr)

                    return hash
                else:
                    print("Test invoke failed")
                    print("TX is %s, results are %s" % (tx, results))
                    return
    # Method from np-prompt but a litlebit edited.
    def test_invoke_contract(self, args):
        if not self.Wallet:
            print("Please open a wallet")
            return

        args, from_addr = get_from_addr(args)
        if args and len(args) > 0:
                # Test invoke contract.
                tx, fee, results, num_ops = TestInvokeContract(self.Wallet, args, from_addr=from_addr)

                if tx is not None and results is not None:
                    result_item = results
                    passwd = prompt("[password]> ", is_password=True)
                    if not self.Wallet.ValidatePassword(passwd):
                        return print("Incorrect password")

                    result = InvokeContract(self.Wallet, tx, fee, from_addr=from_addr)

                    return result_item
                else:
                    print("Error testing contract invoke")
                    return    

                print("Please specify a contract to invoke")
    # Method from np-prompt but a litlebit edited.
    def GatherContractDetails(self,function_code):
        # Fill the contract details.
        name = "AtomSwap"
        version = "0.01"
        author = "TakeWing"
        email = "n.emelin@takewing.ru"
        description = "AtomicSwap"
        
        return generate_deploy_script(function_code.Script, name, version, author, email, description,
                                    function_code.ContractProperties, function_code.ReturnType,
                                    function_code.ParameterList)

    
        
        
