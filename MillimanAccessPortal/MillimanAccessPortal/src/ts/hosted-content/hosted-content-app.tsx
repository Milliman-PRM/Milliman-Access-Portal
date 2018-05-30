import * as React from 'react';
import { Component } from 'react';
//import ContentCard from './content-card';

class HostedContentApp extends Component {
  constructor(props) {
    super(props);
    this.state = {
      contentItems: []
    };
  }

  componentDidMount() {
    const buttonSounds = [];
  }

  render() {
    return (
      <div>
        <h1>It's Working!</h1>
      </div>
    );
  }


}

export default HostedContentApp;
